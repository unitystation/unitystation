using System.Collections;
using Chemistry.Components;
using UnityEngine;
using Mirror;
using Systems.Atmospherics;

namespace Items
{
	/// <summary>
	/// Base class for smokable cigarette
	/// </summary>
	public class Cigarette : NetworkBehaviour, IServerDespawn, ICheckedInteractable<HandApply>,
		ICheckedInteractable<InventoryApply>, IServerInventoryMove
	{
		private const int DEFAULT_SPRITE = 0;
		private const int LIT_SPRITE = 1;

		[SerializeField]
		[Tooltip("Object to spawn after cigarette burnt")]
		private GameObject buttPrefab = null;

		[SerializeField]
		[Tooltip("Time after cigarette will destroy and spawn butt")]
		private float smokeTimeSeconds = 180;

		public SpriteHandler spriteHandler = null;
		private FireSource fireSource = null;
		private Pickupable pickupable = null;

		[SyncVar] private bool isLit = false;
		[SerializeField] private ReagentContainer reagentContainer = null;
		private RegisterPlayer smoker;

		private void Awake()
		{
			pickupable = GetComponent<Pickupable>();
			fireSource = GetComponent<FireSource>();
			reagentContainer ??= GetComponent<ReagentContainer>();
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			ServerChangeLit(false);
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CigBurnLogic);
		}

		#region Interactions

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			// standard validation for interaction
			if (!DefaultWillInteract.Default(interaction, side))
			{
				return false;
			}

			return CheckInteraction(interaction, side);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			TryLightByObject(interaction.UsedObject);
		}

		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			// standard validation for interaction
			if (DefaultWillInteract.Default(interaction, side) == false)
			{
				return false;
			}

			return CheckInteraction(interaction, side);
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			TryLightByObject(interaction.UsedObject);
		}

		private bool CheckInteraction(Interaction interaction, NetworkSide side)
		{
			// check if player want to use some light-source
			if (interaction.UsedObject)
			{
				var lightSource = interaction.UsedObject.GetComponent<FireSource>();
				if (lightSource)
				{
					return true;
				}
			}

			return false;
		}

		#endregion

		private void ServerChangeLit(bool isLitNow)
		{
			// TODO: add support for in-hand and clothing animation
			// update cigarette sprite to lit state
			if (spriteHandler)
			{
				var newSpriteID = isLitNow ? LIT_SPRITE : DEFAULT_SPRITE;
				spriteHandler.SetCatalogueIndexSprite(newSpriteID);
			}

			// toggle flame from cigarette
			if (fireSource)
			{
				fireSource.IsBurning = isLitNow;
			}

			if (isLitNow)
			{
				UpdateManager.Add(CigBurnLogic, smokeTimeSeconds);
			}

			isLit = isLitNow;
		}

		private bool TryLightByObject(GameObject usedObject)
		{
			if (!isLit)
			{
				// check if player tries to lit cigarette with something
				if (usedObject != null)
				{
					// check if it's something like lighter or candle
					var fireSource = usedObject.GetComponent<FireSource>();
					if (fireSource && fireSource.IsBurning)
					{
						ServerChangeLit(true);
						return true;
					}
				}
			}

			return false;
		}

		private void Burn()
		{
			var worldPos = gameObject.AssumedWorldPosServer();
			var tr = gameObject.transform.parent;
			var rotation = RandomUtils.RandomRotation2D();

			// Print burn out message if in players inventory
			if (pickupable && pickupable.ItemSlot != null)
			{
				var player = pickupable.ItemSlot.Player;
				if (player)
				{
					Chat.AddExamineMsgFromServer(player.gameObject,
						$"Your {gameObject.ExpensiveName()} goes out.");
				}
			}

			_ = Despawn.ServerSingle(gameObject);
			Spawn.ServerPrefab(buttPrefab, worldPos, tr, rotation);
		}

		private void CigBurnLogic()
		{
			reagentContainer.Temperature = 300;
			var bigHit = DMMath.Prob(50) ? 5 : 2;
			var burnReagent = reagentContainer.TakeReagents(bigHit);
			if (smoker != null)
			{
				smoker.PlayerScript.playerHealth.reagentPoolSystem.BloodPool.Add(burnReagent);
				Chat.AddExamineMsg(smoker.PlayerScript.gameObject, $"You take a drag out of the {gameObject.ExpensiveName()}");
			}
			if (reagentContainer.ReagentMixTotal.Approx(0)) Burn();
		}

		public void OnInventoryMoveServer(InventoryMove info)
		{
			smoker = null;
			if (info.ToPlayer == null) return;
			if (info.ToSlot == null) return;
			if (info.ToSlot.NamedSlot != NamedSlot.mask) return;
			smoker = info.ToPlayer;
		}
	}
}
