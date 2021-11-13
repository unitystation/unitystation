using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Systems.Explosions;
using ScriptableObjects.Communications;
using Communications;
using Managers;
using Objects;
using UI;
using UnityEngine;


namespace Items.Weapons
{
	public class Explosive : SignalReceiver, ICheckedInteractable<PositionalHandApply>, ICheckedInteractable<MouseDrop>
	{
		[Header("Explosive settings")]
		[SerializeField] private ExplosiveType explosiveType;
		[SerializeField] private bool detonateImmediatelyOnSignal;
		[SerializeField] private int timeToDetonate = 10;
		[SerializeField] private int minimumTimeToDetonate = 10;
		[SerializeField] private ExplosionComponent explosionPrefab;
		[Header("Explosive Components")]
		[SerializeField] private SpriteHandler spriteHandler;
		[SerializeField] private SpriteDataSO activeSpriteSO;
		[SerializeField] private RegisterItem registerItem;
		[SerializeField] private ObjectBehaviour objectBehaviour;
		[SerializeField] private Pickupable pickupable;
		[SerializeField] private HasNetworkTabItem explosiveGUI;
		[HideInInspector] public GUI_Explosive GUI;

		private bool hasExploded;
		private bool isArmed;
		private bool countDownActive = false;
		private bool isOnObject = false;
		public ExplosiveType ExplosiveType => explosiveType;

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

		public bool CountDownActive
		{
			get => countDownActive;
			set => countDownActive = value;
		}


		public int MinimumTimeToDetonate => minimumTimeToDetonate;

		public bool DetonateImmediatelyOnSignal => detonateImmediatelyOnSignal;

		public async void Countdown()
		{
			Debug.Log("We're armed.");
			countDownActive = true;
			spriteHandler.SetSpriteSO(activeSpriteSO);
			if (GUI != null) GUI.StartCoroutine(GUI.UpdateTimer());
			await Task.Delay(timeToDetonate * 1000); //Delay is in millaseconds
			Detonate();
		}

		private void Detonate()
		{
			//We don't use Explosion.StartExplosion() because it doesn't look or work as
			//Explosion prefabs do
			if (hasExploded)
			{
				return;
			}
			hasExploded = true;

			if (isServer)
			{
				// Get data from grenade before despawning
				var explosionMatrix = registerItem.Matrix;
				var worldPos = objectBehaviour.AssumedWorldPositionServer();

				// Despawn grenade
				_ = Despawn.ServerSingle(gameObject);

				// Explosion here
				var explosionGO = Instantiate(explosionPrefab, explosionMatrix.transform);
				explosionGO.transform.position = worldPos;
				explosionGO.Explode(explosionMatrix);
			}
		}

		public override void ReceiveSignal(SignalStrength strength)
		{
			if(countDownActive || isArmed == false) return;
			if (detonateImmediatelyOnSignal)
			{
				Detonate();
				return;
			}
			Countdown();
		}

		private bool CanAttatchToTarget(Matrix matrix, RegisterTile tile)
		{
			return matrix.Get<RegisterDoor>(tile.WorldPositionServer, true).Any() || matrix.Get<Pickupable>(tile.WorldPositionServer, true).Any();
		}


		public bool WillInteract(MouseDrop interaction, NetworkSide side)
		{
			if (interaction.TargetObject.TryGetComponent<SignalEmitter>(out var _) && interaction.IsFromInventory) return true;
			return false;
		}

		public void ServerPerformInteraction(MouseDrop interaction)
		{

			if (interaction.DroppedObject.TryGetComponent<SignalEmitter>(out var signalEmitter))
			{
				Emitter = signalEmitter;
				Chat.AddExamineMsg(interaction.Performer, "You successfully pair the remote signal to the device.");
			}
		}

		public void ToggleMode(bool mode)
		{
			detonateImmediatelyOnSignal = mode;
		}

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side) || isArmed || pickupable.ItemSlot == null && isOnObject == false) return false;
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

					foreach (var registerTile in tiles)
					{
						if (CanAttatchToTarget(registerTile.Matrix, registerTile) == false) continue;
						Chat.AddExamineMsg(interaction.Performer, $"The {interaction.TargetObject.ExpensiveName()} isn't a good spot to arm the C4 on..");
						return;
					}
				}

				Inventory.ServerDrop(pickupable.ItemSlot, interaction.TargetVector);
				isOnObject = true;
				pickupable.ServerSetCanPickup(false);
				spriteHandler.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
				Chat.AddActionMsgToChat(interaction.Performer, $"You attach the {gameObject.ExpensiveName()} to a nearby object..",
					$"{interaction.PerformerPlayerScript.visibleName} attaches a {gameObject.ExpensiveName()} to nearby object!");
			}

			if (pickupable.CanPickup == false && isOnObject == true || interaction.IsAltClick)
			{
				explosiveGUI.ServerPerformInteraction(interaction);
				return;
			}
			var bar = StandardProgressAction.Create(new StandardProgressActionConfig(StandardProgressActionType.CPR, false, false), Perform);
			bar.ServerStartProgress(interaction.Performer.RegisterTile(), 3f, interaction.Performer);
		}
	}

	public enum ExplosiveType
	{
		C4,
		X4
	}
}
