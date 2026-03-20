using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirror;
using UnityEngine;
using US13.ChemistryComponents;
using US13.Core.Chat;
using US13.Core.Addressables.Types;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Sprite_Handler;
using US13.Items.Tool;
using US13.Managers;
using US13.Managers.UpdateManager;
using US13.Messages.Server.SoundMessages;
using US13.Objects.Engineering;
using US13.Systems.Construction;
using US13.Systems.Electricity.Interfaces;
using US13.Systems.Inventory;
using US13.Tilemaps.Behaviours.Objects;
using Util;

namespace US13.Objects.Machines
{
	public class DeepFryer: NetworkBehaviour, IAPCPowerable, IRefreshParts, IExaminable, ICleanable
	{
		[SerializeField] private float oilPerSecond = 0.075f;
		[SerializeField] private float idleWattsConsumption = 5f;
		[SerializeField] private float activeWatsConsumption = 1000f;

		[SerializeField] private ItemStorage storage;

		[SerializeField] private AddressableAudioSource emergeSfx;
		[SerializeField] private AddressableAudioSource fryingLoopSfx1;
		[SerializeField] private AddressableAudioSource fryingLoopSfx2;
		[SerializeField] private AddressableAudioSource dingSfx;

		[Tooltip("How long the frying loop takes to fade in from silence.")]
		[SerializeField] private float crossfadeDuration = 0.75f;

		[SerializeField] private SpriteHandler greaseOverlay;
		[SerializeField] private SpriteHandler leftBasketSprite;
		[SerializeField] private SpriteHandler rightBasketSprite;
		[SerializeField] private float greaseChancePerTick = 50f;
		[SerializeField] private float greaseAmountPerTick = 0.1f;

		private ReagentContainer container;
		private APCPoweredDevice poweredDevice;
		private RegisterTile registerTile;

		/// <summary>
		/// Bitmask tracking which baskets currently have a frying loop playing (server-side).
		/// </summary>
		private byte loopingBaskets;

		[SyncVar(hook = nameof(OnSyncGreasy))]
		private bool isGreasy;

		private string[] basketLoopGUIDs;
		private CancellationTokenSource[] basketLoopCts;

		private float greaseLevel;

		private FryerBasket[] baskets;

		private int laserTier = 1;
		private float oilUse;

		/// <summary>
		/// Fry speed multiplier from the micro-laser tier. Tier 1 = 1x, tier 2 = 2x, etc.
		/// </summary>
		public float FrySpeed => laserTier;

		[field: SyncVar]
		public bool IsPowered { get; private set; }

		public float VoltageModifier { get; private set; } = 1f;

		public FryerBasket GetBasket(int index) => baskets[index];

		public bool HasEnoughOil() => container.ReagentMixTotal >= oilPerSecond;

		private void Awake()
		{
			container = this.GetComponentCustom<ReagentContainer>();
			poweredDevice = this.GetComponentCustom<APCPoweredDevice>();
			registerTile = this.GetComponentCustom<RegisterTile>();
			oilUse = oilPerSecond;

			greaseOverlay.SetCatalogueIndexSprite(0);

			var basketSprites = new[] { leftBasketSprite, rightBasketSprite };
			baskets = new FryerBasket[2];
			basketLoopGUIDs = new string[baskets.Length];
			basketLoopCts = new CancellationTokenSource[baskets.Length];
			for (int i = 0; i < baskets.Length; i++)
			{
				baskets[i] = new FryerBasket(storage.GetIndexedItemSlot(i), this, basketSprites[i]);
				basketLoopGUIDs[i] = "";
			}
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			StopAllBasketLoops();
		}

		private void UpdateMe()
		{
			if (IsPowered == false || baskets == null) return;

			float delta = Time.deltaTime;
			bool anyBasketDown = false;

			for (int i = 0; i < baskets.Length; i++)
			{
				if (baskets[i].State == BasketState.Down)
				{
					anyBasketDown = true;
					baskets[i].Tick(delta, VoltageModifier * FrySpeed);
				}
			}

			if (anyBasketDown)
			{
				AccumulateGrease();
			}
		}

		/// <summary>
		/// Transfers oil from the fryer into the target container (for cookable items that become edible).
		/// </summary>
		[Server]
		public void TransferOilTo(ReagentContainer target, float deltaTime)
		{
			container.TransferTo(oilUse * deltaTime, target);
		}

		public void PowerNetworkUpdate(float voltage) => VoltageModifier = voltage / 240f;

		public void StateUpdate(PowerState state)
		{
			bool wasPowered = IsPowered;
			IsPowered = state != PowerState.Off;

			bool justGainedPower = IsPowered && !wasPowered;
			bool justLostPower = !IsPowered && wasPowered;

			if (justGainedPower)
			{
				UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
				poweredDevice.Wattusage = idleWattsConsumption;
			}
			else if (justLostPower)
			{
				UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
				UpdateLoopStates(0);
				RaiseAllBaskets();
			}
		}

		public void RefreshParts(List<PartReference> partsInFrame, Machine frame)
		{
			foreach (PartReference part in partsInFrame)
			{
				if (part.itemTrait == MachinePartsItemTraits.Instance.MicroLaser)
				{
					laserTier = part.tier;
				}
			}

			// Higher tier laser -> less oil consumed. Matches TG formula.
			oilUse = oilPerSecond - (laserTier * 0.00475f);
		}

		private void RaiseAllBaskets()
		{
			for (int i = 0; i < baskets.Length; i++)
			{
				baskets[i].Raise();
			}
		}

		/// <summary>
		/// Called when a basket state changes so we can update wattage and sound.
		/// </summary>
		public void RefreshWattage()
		{
			if (IsPowered == false) return;

			byte newLoopState = 0;
			bool anyDown = false;
			for (int i = 0; i < baskets.Length; i++)
			{
				if (baskets[i].State == BasketState.Down)
				{
					anyDown = true;
					newLoopState |= (byte)(1 << i);
				}
			}

			poweredDevice.Wattusage = anyDown ? activeWatsConsumption : idleWattsConsumption;
			UpdateLoopStates(newLoopState);
		}

		private void UpdateLoopStates(byte newLoopState)
		{
			for (int i = 0; i < baskets.Length; i++)
			{
				bool wasLooping = (loopingBaskets & (1 << i)) != 0;
				bool isLooping = (newLoopState & (1 << i)) != 0;

				if (wasLooping == isLooping) continue;

				if (isLooping)
				{
					CancelBasketAudio(i);
					StopBasketLoop(i);
					basketLoopCts[i] = new CancellationTokenSource();
					StartLoopWithFadeIn(i, basketLoopCts[i].Token).Forget();
				}
				else
				{
					CancelBasketAudio(i);
					StopBasketLoop(i);
				}
			}

			loopingBaskets = newLoopState;
		}

		#region Play sounds!

		public void PlayEmerge()
		{
			SoundManager.PlayNetworkedAtPos(emergeSfx, registerTile.WorldPosition, sourceObj: gameObject);
		}

		public void PlayDing()
		{
			SoundManager.PlayNetworkedAtPos(dingSfx, registerTile.WorldPosition, sourceObj: gameObject);
		}

		#endregion

		#region Grease

		[Server]
		private void AccumulateGrease()
		{
			if (isGreasy) return;
			if (DMMath.Prob(greaseChancePerTick) == false) return;

			greaseLevel += greaseAmountPerTick;
			if (greaseLevel >= 1f)
			{
				isGreasy = true;
			}
		}

		[Server]
		public void Clean(ICleaner _)
		{
			greaseLevel = 0;
			isGreasy = false;
		}

		private void OnSyncGreasy(bool _, bool newState)
		{
			greaseOverlay.SetCatalogueIndexSprite(newState ? 1 : 0);
		}

		#endregion

		#region Audio

		/// <summary>
		/// Starts the frying loop muted and fades it in over crossfadeDuration.
		/// </summary>
		private async UniTaskVoid StartLoopWithFadeIn(int basketIndex, CancellationToken ct)
		{
			var loopSfx = UnityEngine.Random.value > 0.5f ? fryingLoopSfx1 : fryingLoopSfx2;
			basketLoopGUIDs[basketIndex] = await SoundManager.PlayNetworkedAtPosAsync(loopSfx,
				registerTile.WorldPosition,
				audioSourceParameters: new AudioSourceParameters(pitch: VoltageModifier, isMute: true, loops: true),
				sourceObj: gameObject);

			if (ct.IsCancellationRequested) return;

			// Fade in with discrete volume steps sent to all clients.
			const int steps = 5;
			float stepDuration = crossfadeDuration / steps;
			for (int step = 1; step <= steps; step++)
			{
				if (ct.IsCancellationRequested) return;
				await UniTask.WaitForSeconds(stepDuration, cancellationToken: ct);

				float volume = (float)step / steps;
				ChangeAudioSourceParametersMessage.SendToAll(basketLoopGUIDs[basketIndex],
					new AudioSourceParameters(volume: volume, pitch: VoltageModifier, loops: true));
			}
		}

		private void StopBasketLoop(int basketIndex)
		{
			if (string.IsNullOrEmpty(basketLoopGUIDs[basketIndex]) == false)
			{
				SoundManager.StopNetworked(basketLoopGUIDs[basketIndex]);
				basketLoopGUIDs[basketIndex] = "";
			}
		}

		private void CancelBasketAudio(int basketIndex)
		{
			basketLoopCts[basketIndex]?.Cancel();
			basketLoopCts[basketIndex]?.Dispose();
			basketLoopCts[basketIndex] = null;
		}

		private void StopAllBasketLoops()
		{
			for (int i = 0; i < baskets.Length; i++)
			{
				CancelBasketAudio(i);
				StopBasketLoop(i);
			}
		}

		#endregion

		public void SendLocalMessage(string message)
		{
			Chat.AddLocalMsgToChat(message, gameObject, doSpeechBubble: false);
		}

		public string Examine(Vector3 worldPos = default)
		{
			return IsPowered
				? "A green lead blinks indicating the machine is powered."
				: "The machine seems to be dead, no power";
		}
	}
}
