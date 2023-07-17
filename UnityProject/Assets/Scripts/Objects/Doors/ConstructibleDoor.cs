using Doors.Modules;
using Messages.Server;
using Messages.Server.SoundMessages;
using UnityEngine;
using Objects.Construction;
using Systems.Clearance.Utils;
using Systems.Interaction;

namespace Doors
{
	public class ConstructibleDoor : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		public DoorAnimatorV2 DoorAnimatorV2;

		[Tooltip("Airlock assembly prefab this airlock should deconstruct into.")]
		[SerializeField]
		private GameObject airlockAssemblyPrefab = null;

		public GameObject AirlockAssemblyPrefab => airlockAssemblyPrefab;

		[Tooltip("Prefab of the airlock electronics that lives inside this airlock.")]
		[SerializeField]
		private GameObject airlockElectronicsPrefab = null;

		public GameObject AirlockElectronicsPrefab => airlockElectronicsPrefab;

		public bool Reinforced = false;

		private bool panelopen = false;

		public bool Panelopen => panelopen;

		private DoorMasterController doorMasterController;
		private BoltsModule boltsModule;
		private WeldModule weldModule;
		private Integrity integrity;

		private void Awake()
		{
			doorMasterController = GetComponent<DoorMasterController>();
			boltsModule = GetComponentInChildren<BoltsModule>();
			weldModule = GetComponentInChildren<WeldModule>();

			if (CustomNetworkManager.IsServer == false) return;

			integrity = GetComponent<Integrity>();
			integrity.OnWillDestroyServer.AddListener(WhenDestroyed);

		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side) || interaction.TargetObject != gameObject)
				return false;

			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Screwdriver))
				return true;

			if (Validations.HasUsedComponent<AirlockPainter>(interaction))
				return true;

			if (CheckWeld() && CheckBolts() && doorMasterController.HasPower == false)
			{
				return Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Crowbar);
			}

			return false;
		}


		public bool CheckWeld()
		{
			if (weldModule == null)
			{
				return true;
			}
			else
			{
				return weldModule.IsWelded; //Door has to be welded to allow Deconstruction
			}

		}

		public bool CheckBolts()
		{
			if (boltsModule == null)
			{
				return true;
			}
			else
			{
				return !boltsModule.BoltsDown;
			}
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Screwdriver))
			{
				panelopen = !panelopen;
				if (panelopen)
				{
					DoorAnimatorV2.AddPanelOverlay();
					Chat.AddActionMsgToChat(interaction.Performer,
						$"{interaction.Performer.ExpensiveName()} unscrews {gameObject.ExpensiveName()}'s cable panel.");
				}
				else
				{
					DoorAnimatorV2.RemovePanelOverlay();
					Chat.AddActionMsgToChat(interaction.Performer,
						$"{interaction.Performer.ExpensiveName()} screws in {gameObject.ExpensiveName()}'s cable panel.");

					//Force close net tab when panel is closed
					TabUpdateMessage.SendToPeepers(gameObject, NetTabType.HackingPanel, TabAction.Close);
				}


				AudioSourceParameters audioSourceParameters =
					new AudioSourceParameters(pitch: UnityEngine.Random.Range(0.8f, 1.2f));
				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.screwdriver,
					interaction.Performer.AssumedWorldPosServer(), audioSourceParameters, sourceObj: gameObject);
			}

			if (CheckWeld() && CheckBolts() && !doorMasterController.HasPower)
			{
				if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Crowbar) && airlockAssemblyPrefab)
				{
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
					$"You start to remove electronics from the airlock assembly...",
					$"{interaction.Performer.ExpensiveName()} starts to remove electronics from the airlock assembly...",
					"You removed the airlock electronics from the airlock assembly.",
					$"{interaction.Performer.ExpensiveName()} removed the electronics from the airlock assembly.",
					() => WhenDestroyed(null));
				}
			}
			if (Validations.HasUsedComponent<AirlockPainter>(interaction))
			{
				AirlockPainter painter = interaction.HandObject.GetComponent<AirlockPainter>();
				if (painter)
				{
					ToolUtils.ServerUseToolWithActionMessages(interaction, 3f,
						$"You start to paint the {gameObject.ExpensiveName()}...",
						$"{interaction.Performer.ExpensiveName()} starts to paint the {gameObject.ExpensiveName()}...",
						$"You painted the {gameObject.ExpensiveName()}.",
						$"{interaction.Performer.ExpensiveName()} painted the {gameObject.ExpensiveName()}.",
						() => painter.ServerPaintTheAirlock(gameObject, interaction.Performer));
				}
			}
		}

		public void WhenDestroyed(DestructionInfo info)
		{
			// rare cases were gameObject is destroyed for some reason and then the method is called
			if (gameObject == null) return;
			//Ensure that we cant hit the object in rare cases where two hits can happen quickly before WhenDestroyed() is not invoked or an NRE happens for whatever reason
			if (integrity.Meleeable != null) integrity.Meleeable.IsMeleeable = false;
			//Remove the listener to avoid infinite spawns of objects incase Despawn.ServerSingle() fails for whatever reason
			integrity.OnWillDestroyServer.RemoveListener(WhenDestroyed);

			//When spawning the assembly prefab in the object's place, copy it's access restrictions.
			AccessRestrictions airlockAccess = GetComponentInChildren<AccessRestrictions>();

			//(Max) : This seems like it's prone to error, I recommend making the assembly part inside of the door prefab itself and not another one.
			var doorAssembly = Spawn.ServerPrefab(airlockAssemblyPrefab, SpawnDestination.At(gameObject)).GameObject;
			if (doorAssembly != null && AirlockElectronicsPrefab != null && airlockAccess != null &&
			    doorAssembly.TryGetComponent<AirlockAssembly>(out var assembly))
			{
				assembly.ServerInitFromComputer(AirlockElectronicsPrefab,
					airlockAccess.clearanceRestriction != 0 ? airlockAccess.clearanceRestriction :
						MigrationData.Translation[airlockAccess.restriction], doorMasterController.isWindowedDoor);
			}

			_ = Despawn.ServerSingle(gameObject);
		}
	}
}