using Doors.Modules;
using Messages.Server;
using Messages.Server.SoundMessages;
using UnityEngine;
using Objects.Construction;

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

			if (weldModule.CanDoorStateChange() == false && boltsModule.CanDoorStateChange() && doorMasterController.HasPower == false)
			{
				return Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Crowbar);
			}

			return false;
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
						$"You unscrew the {gameObject.ExpensiveName()}'s cable panel.",
						$"{interaction.Performer.ExpensiveName()} unscrews {gameObject.ExpensiveName()}'s cable panel.");
				}
				else
				{
					DoorAnimatorV2.RemovePanelOverlay();
					Chat.AddActionMsgToChat(interaction.Performer,
						$"You screw in the {gameObject.ExpensiveName()}'s cable panel.",
						$"{interaction.Performer.ExpensiveName()} screws in {gameObject.ExpensiveName()}'s cable panel.");

					//Force close net tab when panel is closed
					TabUpdateMessage.SendToPeepers(gameObject, NetTabType.HackingPanel, TabAction.Close);
				}


				AudioSourceParameters audioSourceParameters =
					new AudioSourceParameters(pitch: UnityEngine.Random.Range(0.8f, 1.2f));
				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.screwdriver,
					interaction.Performer.AssumedWorldPosServer(), audioSourceParameters, sourceObj: gameObject);
			}

			if (!weldModule.CanDoorStateChange() && boltsModule.CanDoorStateChange() && !doorMasterController.HasPower)
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

			AccessRestrictions airlockAccess = GetComponentInChildren<AccessRestrictions>();

			var doorAssembly = Spawn.ServerPrefab(airlockAssemblyPrefab, SpawnDestination.At(gameObject)).GameObject;
			doorAssembly.GetComponent<AirlockAssembly>().ServerInitFromComputer(AirlockElectronicsPrefab, airlockAccess.restriction, doorMasterController.isWindowedDoor);
			_ = Despawn.ServerSingle(gameObject);

			integrity.OnWillDestroyServer.RemoveListener(WhenDestroyed);
		}
	}
}