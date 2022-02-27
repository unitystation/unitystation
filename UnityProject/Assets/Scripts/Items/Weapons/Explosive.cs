using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Mirror;
using Communications;
using Managers;
using Systems.Explosions;
using Scripts.Core.Transform;
using UI.Items;

namespace Items.Weapons
{
	public class Explosive : SignalReceiver, ICheckedInteractable<PositionalHandApply>, IRightClickable, IInteractable<InventoryApply>
	{
		[Header("Explosive settings")]
		[SerializeField] private ExplosiveType explosiveType;
		[SerializeField] private bool detonateImmediatelyOnSignal;
		[SerializeField] private int timeToDetonate = 10;
		[SerializeField] private int minimumTimeToDetonate = 10;
		[SerializeField] private float explosiveStrength = 150f;
		[SerializeField] private SpriteDataSO activeSpriteSO;
		[SerializeField] private float progressTime = 3f;
		[Header("Explosive Components")]
		[SerializeField] private SpriteHandler spriteHandler;
		[SerializeField] private ScaleSync scaleSync;
		private RegisterItem registerItem;
		private ObjectBehaviour objectBehaviour;
		private Pickupable pickupable;
		private HasNetworkTabItem explosiveGUI;
		[HideInInspector] public GUI_Explosive GUI;

		[SyncVar] private bool isArmed;
		[SyncVar] private bool countDownActive = false;
		[SyncVar] private bool isOnObject = false;
		private GameObject attachedToObject;

		public int TimeToDetonate
		{
			get => timeToDetonate;
			set => timeToDetonate = value;
		}

		public bool IsArmed
		{
			get => isArmed;
			set => isArmed = value;
		}

		public int MinimumTimeToDetonate => minimumTimeToDetonate;
		public bool DetonateImmediatelyOnSignal => detonateImmediatelyOnSignal;
		public bool CountDownActive => countDownActive;
		public ExplosiveType ExplosiveType => explosiveType;

		private void Awake()
		{
			if(spriteHandler == null) spriteHandler = GetComponentInChildren<SpriteHandler>();
			if(scaleSync == null) scaleSync = GetComponent<ScaleSync>();
			registerItem = GetComponent<RegisterItem>();
			objectBehaviour = GetComponent<ObjectBehaviour>();
			pickupable = GetComponent<Pickupable>();
			explosiveGUI = GetComponent<HasNetworkTabItem>();
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateBombPosition);
		}

		[Command(requiresAuthority = false)]
		public void CmdTellServerToCountDown()
		{
			StartCoroutine(Countdown());
		}

		[Server]
		public IEnumerator Countdown()
		{
			countDownActive = true;
			spriteHandler.SetSpriteSO(activeSpriteSO);
			if (GUI != null) GUI.StartCoroutine(GUI.UpdateTimer());
			yield return WaitFor.Seconds(timeToDetonate); //Delay is in milliseconds
			Detonate();
		}

		private void Detonate()
		{
			// Get data before despawning
			var worldPos = objectBehaviour.AssumedWorldPositionServer();
			// Despawn the explosive
			_ = Despawn.ServerSingle(gameObject);
			Explosion.StartExplosion(worldPos, explosiveStrength);
		}

		[Server]
		private void AttachExplosive(GameObject target, Vector2 targetPostion)
		{
			if (target.TryGetComponent<PushPull>(out var handler))
			{
				Inventory.ServerDrop(pickupable.ItemSlot, targetPostion);
				attachedToObject = target;
				UpdateManager.Add(UpdateBombPosition, 0.1f);
				scaleSync.SetScale(new Vector3(0.6f, 0.6f, 0.6f));
				return;
			}

			Inventory.ServerDrop(pickupable.ItemSlot, targetPostion);
			//Visual feedback to indicate that it's been attached and not just dropped.
			scaleSync.SetScale(new Vector3(0.6f, 0.6f, 0.6f));
		}

		private void UpdateBombPosition()
		{
			if(attachedToObject == null) return;
			if(attachedToObject.WorldPosServer() == gameObject.WorldPosServer()) return;
			registerItem.customNetTransform.SetPosition(attachedToObject.WorldPosServer());
		}

		/// <summary>
		/// Toggle the detention mode of the explosive, true means it will only work with a signaling device.
		/// </summary>
		/// <param name="mode"></param>
		public void ToggleMode(bool mode)
		{
			detonateImmediatelyOnSignal = mode;
		}

		[Command(requiresAuthority = false)]
		private void CmdTellServerToDeattachExplosive()
		{
			DeAttachExplosive();
		}

		[Server]
		private void DeAttachExplosive()
		{
			isOnObject = false;
			pickupable.ServerSetCanPickup(true);
			objectBehaviour.ServerSetPushable(true);
			scaleSync.SetScale(new Vector3(1f, 1f, 1f));
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateBombPosition);
			attachedToObject = null;
		}

		public override void ReceiveSignal(SignalStrength strength, ISignalMessage message = null)
		{
			if(countDownActive == true || isArmed == false) return;
			if (detonateImmediatelyOnSignal)
			{
				Detonate();
				return;
			}
			StartCoroutine(Countdown());
		}

		/// <summary>
		/// checks to see if we can attach the explosive to an object.
		/// </summary>
		private bool CanAttatchToTarget(Matrix matrix, RegisterTile tile)
		{
			return matrix.Get<RegisterDoor>(tile.WorldPositionServer, true).Any() || matrix.Get<Pickupable>(tile.WorldPositionServer, true).Any();
		}

		#region Interaction

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false
			    || isArmed == true || pickupable.ItemSlot == null && isOnObject == false) return false;
			if (interaction.TargetObject.TryGetComponent<SignalEmitter>(out var em)) return true;
			return true;
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			void Perform()
			{
				if (interaction.TargetObject?.OrNull().RegisterTile()?.OrNull().Matrix?.OrNull() != null)
				{
					var matrix = interaction.TargetObject.RegisterTile().Matrix;
					var tiles = matrix.GetRegisterTile(interaction.TargetObject.TileLocalPosition().To3Int(), true);
					//Check to see if we're trying to attach the object to things we're not supposed to
					//because we don't want stuff like this happening :
					//https://youtu.be/0Yu8hEBMRwc
					foreach (var registerTile in tiles)
					{
						if (CanAttatchToTarget(registerTile.Matrix, registerTile) == false) continue;
						Chat.AddExamineMsg(interaction.Performer, $"The {interaction.TargetObject.ExpensiveName()} isn't a good spot to arm the explosive on..");
						return;
					}
				}

				AttachExplosive(interaction.TargetObject, interaction.TargetVector);
				isOnObject = true;
				pickupable.ServerSetCanPickup(false);
				objectBehaviour.ServerSetPushable(false);
				Chat.AddActionMsgToChat(interaction.Performer, $"You attach the {gameObject.ExpensiveName()} to {interaction.TargetObject.ExpensiveName()}",
					$"{interaction.PerformerPlayerScript.visibleName} attaches a {gameObject.ExpensiveName()} to {interaction.TargetObject.ExpensiveName()}!");
			}

			//For interacting with the explosive while it's on a wall.
			if (isOnObject || interaction.IsAltClick)
			{
				explosiveGUI.ServerPerformInteraction(interaction);
				return;
			}

			//incase we forgot to pair while the C4 is on the wall
			if (interaction.TargetObject.TryGetComponent<SignalEmitter>(out var emitter))
			{
				Emitter = emitter;
				Frequency = emitter.Frequency;
				Chat.AddExamineMsg(interaction.Performer, "You successfully pair the remote signal to the device.");
				return;
			}

			//The progress bar that triggers Preform()
			//Must not be interrupted for it to work.
			var bar = StandardProgressAction.Create(new StandardProgressActionConfig(StandardProgressActionType.CPR, false, false), Perform);
			bar.ServerStartProgress(interaction.Performer.RegisterTile(), progressTime, interaction.Performer);
		}
		#endregion

		public RightClickableResult GenerateRightClickOptions()
		{
			RightClickableResult result = new RightClickableResult();
			if (isOnObject == false) return result;
			if (CustomNetworkManager.IsServer)
			{
				result.AddElement("Deattach", DeAttachExplosive);
			}
			else
			{
				result.AddElement("Deattach", CmdTellServerToDeattachExplosive);
			}
			return result;
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			if (interaction.TargetSlot.IsEmpty == false)
			{
				if (interaction.TargetSlot.ItemObject.TryGetComponent<SignalEmitter>(out var emitter))
				{
					Emitter = emitter;
					Frequency = emitter.Frequency;
					Chat.AddExamineMsg(interaction.Performer, "You successfully pair the remote signal to the device.");
				}
			}
		}
	}

	public enum ExplosiveType
	{
		C4,
		X4
	}
}
