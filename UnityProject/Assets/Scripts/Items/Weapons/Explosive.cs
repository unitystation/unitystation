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
		[SerializeField] private SpriteDataSO activeSpriteSO;
		[Header("Explosive Components")]
		private SpriteHandler spriteHandler;
		private RegisterItem registerItem;
		private ObjectBehaviour objectBehaviour;
		private Pickupable pickupable;
		private HasNetworkTabItem explosiveGUI;
		[HideInInspector] public GUI_Explosive GUI;

		private bool hasExploded;
		private bool isArmed;
		private bool countDownActive = false;
		private bool isOnObject = false;

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
			spriteHandler = GetComponent<SpriteHandler>();
			registerItem = GetComponent<RegisterItem>();
			objectBehaviour = GetComponent<ObjectBehaviour>();
			pickupable = GetComponent<Pickupable>();
			explosiveGUI = GetComponent<HasNetworkTabItem>();
		}

		public async void Countdown()
		{
			countDownActive = true;
			spriteHandler.SetSpriteSO(activeSpriteSO);
			if (GUI != null) GUI.StartCoroutine(GUI.UpdateTimer());
			await Task.Delay(timeToDetonate * 1000); //Delay is in milliseconds
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
				// Get data before despawning
				var explosionMatrix = registerItem.Matrix;
				var worldPos = objectBehaviour.AssumedWorldPositionServer();

				// Despawn the explosive
				_ = Despawn.ServerSingle(gameObject);

				// Explosion here
				var explosionGO = Instantiate(explosionPrefab, explosionMatrix.transform);
				explosionGO.transform.position = worldPos;
				explosionGO.Explode(explosionMatrix);
			}
		}

		/// <summary>
		/// Toggle the detention mode of the explosive, true means it will only work with a signaling device.
		/// </summary>
		/// <param name="mode"></param>
		public void ToggleMode(bool mode)
		{
			detonateImmediatelyOnSignal = mode;
		}

		public override void ReceiveSignal(SignalStrength strength)
		{
			if(countDownActive == true || isArmed == false) return;
			if (detonateImmediatelyOnSignal)
			{
				Detonate();
				return;
			}
			Countdown();
		}

		/// <summary>
		/// checks to see if we can attach the explosive to an object.
		/// </summary>
		private bool CanAttatchToTarget(Matrix matrix, RegisterTile tile)
		{
			return matrix.Get<RegisterDoor>(tile.WorldPositionServer, true).Any() || matrix.Get<Pickupable>(tile.WorldPositionServer, true).Any();
		}

		#region Interaction

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

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false
			    || isArmed == true || pickupable.ItemSlot == null && isOnObject == false) return false;
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
						Chat.AddExamineMsg(interaction.Performer, $"The {interaction.TargetObject.ExpensiveName()} isn't a good spot to arm the C4 on..");
						return;
					}
				}

				Inventory.ServerDrop(pickupable.ItemSlot, interaction.TargetVector);
				isOnObject = true;
				pickupable.ServerSetCanPickup(false);
				spriteHandler.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f); //Visual feedback to indicate that it's been attached and not just dropped.
				Chat.AddActionMsgToChat(interaction.Performer, $"You attach the {gameObject.ExpensiveName()} to a nearby object..",
					$"{interaction.PerformerPlayerScript.visibleName} attaches a {gameObject.ExpensiveName()} to nearby object!");
			}

			//For interacting with the explosive while it's on a wall.
			if (pickupable.CanPickup == false && isOnObject == true || interaction.IsAltClick)
			{
				explosiveGUI.ServerPerformInteraction(interaction);
				return;
			}

			//incase we forgot to pair while the C4 is on the wall
			if (isOnObject && detonateImmediatelyOnSignal &&
			    interaction.HandObject.TryGetComponent<SignalEmitter>(out var emitter))
			{
				Emitter = emitter;
				Chat.AddExamineMsg(interaction.Performer, "You successfully pair the remote signal to the device.");
			}
			//The progress bar that triggers Preform()
			//Must not be interrupted for it to work.
			var bar = StandardProgressAction.Create(new StandardProgressActionConfig(StandardProgressActionType.CPR, false, false), Perform);
			bar.ServerStartProgress(interaction.Performer.RegisterTile(), 3f, interaction.Performer);
		}
		#endregion
	}

	public enum ExplosiveType
	{
		C4,
		X4
	}
}
