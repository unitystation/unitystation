using System.Collections.Generic;
using System.Linq;
using System.Text;
using Doors;
using Doors.Modules;
using Messages.Server;
using Messages.Server.SoundMessages;
using Objects.Construction;
using Systems.Clearance;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine;

namespace Objects.Doors.DoorDeconstruction
{
	public class ConstructibleDoor : MonoBehaviour, ICheckedInteractable<HandApply>, IHoverTooltip
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

		[SerializeField] private bool allowHackingPanel = true;
		public bool AllowHackingPanel => allowHackingPanel;

		public bool AllowTampering = true;

		private DoorMasterController doorMasterController;
		private BoltsModule boltsModule;
		private WeldModule weldModule;
		private PowerModule powerModule;
		private Integrity integrity;


		[SerializeReference, Core.Editor.Attributes.SelectImplementation(typeof(IDeconstructionMethod))]
		public List<IDeconstructionMethod> DeconstructionMethods = new List<IDeconstructionMethod>();

		private void Awake()
		{
			doorMasterController = GetComponent<DoorMasterController>();
			boltsModule = GetComponentInChildren<BoltsModule>();
			weldModule = GetComponentInChildren<WeldModule>();
			powerModule = GetComponentInChildren<PowerModule>();

			if (CustomNetworkManager.IsServer == false) return;

			integrity = GetComponent<Integrity>();
			integrity.OnWillDestroyServer.AddListener(WhenDestroyed);

		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (AllowTampering == false) return false;
			if (!DefaultWillInteract.Default(interaction, side) || interaction.TargetObject != gameObject) return false;

			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Screwdriver)) return true;
			if (Validations.HasUsedComponent<AirlockPainter>(interaction)) return true;

			if (Panelopen && AllowHackingPanel && (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Cable) ||
					Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Wirecutter)))
				return true;

			// What deconstruction methods does this door use?
			if (DeconstructionMethods != null)
			{
				foreach (var method in DeconstructionMethods)
				{
					if (method != null && method.CanInteract(this, interaction, side))
						return true;
				}
			}

			return false;
		}


		public bool CheckWeld()
		{
			if (weldModule == null)
			{
				return true;
			}
			return weldModule.IsWelded; //Door has to be welded to allow Deconstruction
		}

		public bool IsWeldedShut()
		{
			if (weldModule == null)
			{
				return false;
			}
			return weldModule.IsWelded;
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

		public bool HasBoltsDown()
		{
			if (boltsModule == null)
			{
				return false;
			}
			return boltsModule.BoltsDown;
		}

		public bool CheckPower()
		{
			if (powerModule == null)
			{
				return true;
			}
			//(Max): Confusing method name, returns true if there is NO power.
			return !powerModule.HasPower;
		}

		public bool HasPower()
		{
			if (powerModule == null)
			{
				return false;
			}
			return powerModule.HasPower;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (DeconstructionMethods != null)
			{
				foreach (var method in DeconstructionMethods)
				{
					if (method != null && method.CanInteract(this, interaction, NetworkSide.Server))
					{
						method.ServerPerform(this, interaction);
						return;
					}
				}
			}

			if (Panelopen && AllowHackingPanel)
			{
				if (IsTryingToHackDoor(interaction)) return;
			}

			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Screwdriver) && AllowHackingPanel)
			{
				MessWithPanel(interaction);
				return;
			}

			if (Validations.HasUsedComponent<AirlockPainter>(interaction))
			{
				PaintDoor(interaction);
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
			ClearanceRestricted airlockAccess = GetComponentInChildren<ClearanceRestricted>();

			//(Max) : This seems like it's prone to error, I recommend making the assembly part inside of the door prefab itself and not another one.
			var doorAssembly = Spawn.ServerPrefab(airlockAssemblyPrefab, SpawnDestination.At(gameObject)).GameObject;
			if (doorAssembly != null && AirlockElectronicsPrefab != null && airlockAccess != null &&
			    doorAssembly.TryGetComponent<AirlockAssembly>(out var assembly))
			{
				assembly.ServerInitFromComputer(AirlockElectronicsPrefab,
					airlockAccess.RequiredClearance.FirstOrDefault(), doorMasterController.isWindowedDoor);
			}

			_ = Despawn.ServerSingle(gameObject);
		}

		private bool IsTryingToHackDoor(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Cable) ||
			    Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Wirecutter))
			{
				TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType.HackingPanel, TabAction.Open);
				return true;
			}

			return false;
		}

		private void MessWithPanel(HandApply interaction)
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

		private void PaintDoor(HandApply interaction)
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

		public string HoverTip()
		{
			StringBuilder tips = new();
			if (allowHackingPanel)
			{
				var panelIsOpen = panelopen ? "Open" : "Closed";
				tips.AppendLine($"The interacted panel is currently {panelIsOpen}.");
			}

			// Append tips from deconstruction methods
			if (DeconstructionMethods != null && AllowTampering)
			{
				foreach (var method in DeconstructionMethods)
				{
					if (method == null) continue;
					var tip = method.HoverTip(this);
					if (!string.IsNullOrEmpty(tip)) tips.AppendLine(tip);
				}
			}

			return tips.ToString();
		}

		public string CustomTitle()
		{
			return null;
		}

		public Sprite CustomIcon()
		{
			return null;
		}

		public List<Sprite> IconIndicators()
		{
			return null;
		}

		public List<TextColor> InteractionsStrings()
		{
			var tips = new List<TextColor>();
			if (AllowTampering == false) return tips;

			if (AllowHackingPanel)
			{
				tips.Add(new TextColor{ Text = "You can use a <b>screwdriver</b> to unlock the panel for cutting wires, or deconstructing the door.", Color = Color.green} );
			}
			if (Panelopen)
			{
				tips.Add(new TextColor{ Text = "Cut wires using a wire-cutter.", Color = Color.green});
			}

			// Merge interaction tips from deconstruction methods
			if (DeconstructionMethods != null)
			{
				foreach (var method in DeconstructionMethods)
				{
					if (method == null) continue;
					var methodTips = method.InteractionStrings(this);
					if (methodTips != null) tips.AddRange(methodTips);
				}
			}

			return tips;
		}
	}
}

