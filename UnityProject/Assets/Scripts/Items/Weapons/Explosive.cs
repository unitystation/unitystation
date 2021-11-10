using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Systems.Explosions;
using ScriptableObjects.Communications;
using Communications;
using Managers;
using UnityEngine;


namespace Items.Weapons
{
	public class Explosive : SignalReceiver, ICheckedInteractable<HandApply>, ICheckedInteractable<MouseDrop>, IInteractable<HandActivate>
	{
		[Header("Explosive settings")]
		[SerializeField] private bool detonateImmediatelyOnSignal;
		[SerializeField] private int timeToDetonate = 10;
		[SerializeField] private ExplosionComponent explosionPrefab;
		[Header("Explosive Components")]
		[SerializeField] private SpriteHandler spriteHandler;
		[SerializeField] private SpriteDataSO activeSpriteSO;
		[SerializeField] private RegisterItem registerItem;
		[SerializeField] private ObjectBehaviour objectBehaviour;
		[SerializeField] private Pickupable pickupable;

		private bool hasExploded;
		private bool isArmed;
		private bool countDownOnArm = false;
		private bool countDownActive = false;

		private async void Countdown()
		{
			countDownActive = true;
			spriteHandler.SetSpriteSO(activeSpriteSO);
			await Task.Delay(timeToDetonate * 1000); //Delay is in millaseconds
			Detonate();
		}

		private void Detonate()
		{
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
			return matrix.IsWallAt(tile.WorldPosition, true) ||
			       matrix.IsWindowAt(tile.WorldPosition, true) ||
			       matrix.IsGrillAt(tile.WorldPosition, true) ||
			       matrix.Get<RegisterDoor>(tile.WorldPosition, true).Any();
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

		public void ServerPerformInteraction(HandActivate interaction)
		{
			countDownOnArm = !countDownOnArm;
			if (countDownOnArm)
			{
				Chat.AddExamineMsg(interaction.Performer, "The C4 will start counting down as soon it's armed on a wall.");
				return;
			}
			Chat.AddExamineMsg(interaction.Performer, "The C4 will wait for a signal from a remote when armed on a wall.");
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			Debug.Log($"{interaction.TargetObject} - {interaction.TargetObject.WorldPosServer()}");
			if (!DefaultWillInteract.Default(interaction, side) || pickupable.ItemSlot == null) return false;
			var matrix = interaction.TargetObject.RegisterTile().Matrix;
			var tiles = matrix.GetRegisterTile(interaction.TargetObject.WorldPosServer().RoundToInt(), true);
			foreach (var registerTile in tiles)
			{
				if (CanAttatchToTarget(registerTile.Matrix, registerTile))
				{
					return true;
				}
			}
			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			void Perform()
			{
				Debug.Log("handApply found walls and looking for the correct one");
				var matrix = interaction.TargetObject.RegisterTile().Matrix;
				var tiles = matrix.GetRegisterTile(interaction.TargetObject.WorldPosServer().RoundToInt(), true);
				foreach (var registerTile in tiles)
				{
					if (CanAttatchToTarget(registerTile.Matrix, registerTile))
					{
						Inventory.ServerDrop(pickupable.ItemSlot, interaction.TargetObject.AssumedWorldPosServer());
						if (countDownOnArm)
						{
							pickupable.ServerSetCanPickup(false);
							spriteHandler.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
						}
						isArmed = true;
						Chat.AddActionMsgToChat(interaction.Performer, $"You attach the {gameObject.ExpensiveName()} to the {registerTile.gameObject.ExpensiveName()}",
							$"{interaction.PerformerPlayerScript.visibleName} attaches a {gameObject.ExpensiveName()} to the {registerTile.gameObject.ExpensiveName()}");
						Countdown();
						return;
					}
					Chat.AddExamineMsg(interaction.Performer, $"The {interaction.TargetObject.ExpensiveName()} isn't a good spot to arm the C4 on..");
				}
			}
			var bar = StandardProgressAction.Create(new StandardProgressActionConfig(StandardProgressActionType.CPR, false, false), Perform);
			bar.ServerStartProgress(interaction.Performer.RegisterTile(), 3f, interaction.Performer);
		}
	}
}
