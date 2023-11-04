using UnityEngine;
using Systems.Hacking;
using Messages.Server;
using Messages.Server.SoundMessages;
using Systems.Electricity;
using UI.Core.Net;

namespace Objects.Construction
{
	/// <summary>
	/// Main behavior for computers. Allows them to be deconstructed to frames.
	/// </summary>
	public class Computer : MonoBehaviour, ICheckedInteractable<HandApply>, IAPCPowerable, ICanOpenNetTab
	{
		[Tooltip("Frame prefab this computer should deconstruct into.")]
		[SerializeField]
		private GameObject framePrefab = null;

		[Tooltip("Prefab of the circuit board that lives inside this computer.")]
		[SerializeField]
		private GameObject circuitBoardPrefab = null;

		/// <summary>
		/// Prefab of the circuit board that lives inside this computer.
		/// </summary>
		public GameObject CircuitBoardPrefab => circuitBoardPrefab;

		public bool hasPower = false;

		[Tooltip("Time taken to screwdrive to deconstruct this.")]
		[SerializeField]
		private float secondsToScrewdrive = 2f;

		/// <summary>
		/// Can this console not be deconstructed?
		/// </summary>
		[SerializeField]
		private bool canNotBeDeconstructed;

		private Integrity integrity;

		private bool panelopen = false;

		private HackingProcessBase HackingProcessBase;

		private void Awake()
		{
			HackingProcessBase = GetComponent<HackingProcessBase>();
			if (CustomNetworkManager.IsServer == false) return;

			integrity = GetComponent<Integrity>();

			integrity.OnWillDestroyServer.AddListener(WhenDestroyed);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (!Validations.IsTarget(gameObject, interaction)) return false;

			if (HackingProcessBase != null)
			{
				return Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver) ||
				       Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar) || //Should probably network if it is open or not
				       Validations.HasItemTrait(interaction, CommonTraits.Instance.Cable) ||
				       Validations.HasItemTrait(interaction, CommonTraits.Instance.Wirecutter);
			}
			else
			{
				return Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver) ||
				       Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar);
			}
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver))
			{
				AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: UnityEngine.Random.Range(0.8f, 1.2f));
				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.screwdriver, interaction.Performer.AssumedWorldPosServer(), audioSourceParameters, sourceObj: gameObject);
				//Unscrew panel
				panelopen = !panelopen;
				if (panelopen)
				{
					Chat.AddActionMsgToChat(interaction.Performer,
						$"You unscrews the {gameObject.ExpensiveName()}'s cable panel.",
						$"{interaction.Performer.ExpensiveName()} unscrews {gameObject.ExpensiveName()}'s cable panel.");
					return;
				}
				else
				{
					Chat.AddActionMsgToChat(interaction.Performer,
						$"You screw in the {gameObject.ExpensiveName()}'s cable panel.",
						$"{interaction.Performer.ExpensiveName()} screws in {gameObject.ExpensiveName()}'s cable panel.");
					return;
				}
			}

			if (HackingProcessBase != null)
			{
				if (panelopen && (Validations.HasItemTrait(interaction, CommonTraits.Instance.Cable) ||
				                  Validations.HasItemTrait(interaction, CommonTraits.Instance.Wirecutter)))
				{
					TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType.HackingPanel, TabAction.Open);
				}
			}

			//unsecure
			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar) && panelopen)
			{
				if (canNotBeDeconstructed)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, "This console cannot be deconstructed!");
					return;
				}

				ToolUtils.ServerUseToolWithActionMessages(interaction, secondsToScrewdrive,
					"You start to disconnect the monitor...",
					$"{interaction.Performer.ExpensiveName()} starts to disconnect the monitor...",
					"You disconnect the monitor.",
					$"{interaction.Performer.ExpensiveName()} disconnects the monitor.",
					() => { WhenDestroyed(null); });
			}
		}

		public void WhenDestroyed(DestructionInfo info)
		{
			//drop all our contents
			ItemStorage itemStorage = null;
			// rare cases were gameObject is destroyed for some reason and then the method is called
			if (gameObject == null) return;

			itemStorage = GetComponent<ItemStorage>();

			if (itemStorage != null)
			{
				itemStorage.ServerDropAll();
			}
			var frame = Spawn.ServerPrefab(framePrefab, SpawnDestination.At(gameObject)).GameObject;
			frame.GetComponent<ComputerFrame>().ServerInitFromComputer(this);
			_ = Despawn.ServerSingle(gameObject);

			integrity.OnWillDestroyServer.RemoveListener(WhenDestroyed);
		}

		public void PowerNetworkUpdate(float voltage) {}

		public void StateUpdate(PowerState state)
		{
			if (state == PowerState.Off || state == PowerState.LowVoltage)
			{
				hasPower = false;
			}
			else
			{
				hasPower = true;
			}
			GetComponent<ConsoleScreenAnimator>().ToggleOn(hasPower);
		}

		public bool CanOpenNetTab(GameObject playerObject, NetTabType netTabType)
		{
			if (!hasPower)
			{
				Chat.AddExamineMsgFromServer(playerObject, $"{gameObject.ExpensiveName()} is unpowered");
				return false;
			}
			return true;
		}
	}
}
