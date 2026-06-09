using System.Collections.Generic;
using System.Linq;
using System.Text;
using Logs;
using Mirror;
using NaughtyAttributes;
using UnityEngine;
using US13.Core;
using US13.Core.Addressables.Types;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.ObjectConnection;
using US13.Items.Traits;
using US13.Managers;
using US13.Managers.MatrixManager;
using US13.Managers.NetworkManagement;
using US13.Managers.UpdateManager;
using US13.Messages.Server.SoundMessages;
using US13.UI.Systems.Tooltips.HoverTooltips;
using Util;
using Util.Independent.FluentRichText;
using UniversalObjectPhysics = US13.Core.Physics.UniversalObjectPhysics;

namespace US13.Systems.Electricity.PowerSupplies
{
	[RequireComponent(typeof(UniversalObjectPhysics))]
	public sealed class SolarPanel : NetworkBehaviour, ICheckedInteractable<HandApply>, IMultitoolSlaveable, IExaminable, IHoverTooltip
	{
		[BoxGroup("References")]
		[SerializeField] private AddressableAudioSource warningBeeper;
		[SerializeField] private AddressableAudioSource wrenchSound;
		[field: SerializeField] public UniversalObjectPhysics Physics { get; private set; }

		[BoxGroup("Power Production Settings")]
		[SerializeField] private float updateRate = 16f;
		[SerializeField] private int productionPowerPointsPerFreeAvaliableSide = 150;
		[SerializeField] private int wattsMultiplierPerPowerPoint = 2;
		[SerializeField] private bool isOn = true;

		[BoxGroup("Misc")]
		[SerializeField, Tooltip("This is in case the map saver is unable to serialize the controller link in the future.")]
		private bool automaticallyLinkToNearestControllerOnStart = false;
		[SerializeField, Tooltip("1 unit = 1 tile.\nThis is the radius the panel will search for a controller on start if autoLink is enabled.")]
		private float searchRadiusForControllers = 250;

		[field: SerializeField] public int LastProducedWatts { get; private set; } = 0;

		private AudioSourceParameters beeperSettings = new AudioSourceParameters()
		{
			Volume = 0.5f,
		};

		public MultitoolConnectionType ConType { get; } = MultitoolConnectionType.SolarPanel;
		public bool CanRelink { get; } = true;
		public bool RequireLink { get; } = true;
		public IMultitoolMasterable Master { get; set; }
		public SolarPanelController Controller;

		private void Awake()
		{
			Physics ??= GetComponent<UniversalObjectPhysics>();
			if (CustomNetworkManager.IsServer) UpdateManager.Add(UpdateMe, updateRate);
			if (Controller != null)
			{
				Controller.AddDevice(this);
				Master = Controller;
			}
			else
			{
				isOn = false;
			}
		}

		private void Start()
		{
			GetNearestControllerAndSetIt();
		}

		private void OnDestroy()
		{
			if (CustomNetworkManager.IsServer) UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			if (isOn == false) return;
			var points = CalculatePoints();
			LastProducedWatts = points * wattsMultiplierPerPowerPoint;
			if (points == 0)
			{
				if (warningBeeper != null) SoundManager.PlayNetworkedAtPos(warningBeeper, gameObject.AssumedWorldPosServer(), beeperSettings);
			}
		}

		private int CalculatePoints()
		{
			var points = 0;
			Vector3Int currentPos = gameObject.AssumedWorldPosServer().CutToInt();
			Vector3Int[] directions = new Vector3Int[]
			{
				new Vector3Int(0, 1),   // Up
				new Vector3Int(0, -1),  // Down
				new Vector3Int(-1, 0),  // Left
				new Vector3Int(1, 0)    // Right
			};
			foreach (Vector3Int direction in directions)
			{
				Vector3Int neighborPos = currentPos + direction;
				if (MatrixManager.IsSpaceAt(neighborPos, true)) points += productionPowerPointsPerFreeAvaliableSide;
			}
			return points;
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.HandObject == null) return;
			var traits = interaction.HandSlot.ItemAttributes.GetTraits();
			if (traits.Contains(CommonTraits.Instance.Wrench) == false) return;
			Physics.SetIsNotPushable(!Physics.isNotPushable);
			UpdatePushableStateProperties();
			if (wrenchSound != null) SoundManager.PlayNetworkedAtPos(wrenchSound, gameObject.AssumedWorldPosServer());
			Chat.AddActionMsgToChat(gameObject, $"The {gameObject.ExpensiveName()} turns {OnOff()} as its bolt make a clicking sound.");
		}

		private void UpdatePushableStateProperties()
		{
			isOn = Physics.isNotPushable;
			if (isOn == false)
			{
				Controller?.RemoveDevice(this);
				Master = null;
			}
		}

		public bool TrySetMaster(GameObject performer, IMultitoolMasterable master)
		{
			if (master.gameObject.TryGetComponent<SolarPanelController>(out var controller) == false) return false;
			return controller.AddDevice(this);
		}

		public void SetMasterEditor(IMultitoolMasterable master)
		{
			if (master.gameObject.TryGetComponent<SolarPanelController>(out var controller) == false) return;
			Master = controller;
			Controller = controller;
			controller.AddDevice(this);
		}

		private void GetNearestControllerAndSetIt()
		{
			if (automaticallyLinkToNearestControllerOnStart == false) return;
			var nearbyControllers =
				ComponentsTracker<SolarPanelController>.GetAllNearbyTypesToTarget(gameObject, searchRadiusForControllers);
			var controller = nearbyControllers?.FirstOrDefault();
			if (controller == null)
			{
				Loggy.Warning(
					$"Attempted to auto-link Solar Panel '{gameObject.ExpensiveName()}' to nearest controller," +
					$" but none were found in radius {searchRadiusForControllers}.");
				return;
			}
			Master = controller;
			controller.AddDevice(this);
		}

		private string OnOff()
		{
			return isOn ? "on" : "off";
		}

		public string Examine(Vector3 worldPos = default)
		{
			var inital = $"This panel is currently {OnOff()}.\n It's screen reads out: {LastProducedWatts}.";
			if (Controller == null)
				inital += "\nHowever, This panel is not connected to any controllers to siphon the produced energy.";
			return inital;
		}

		public string HoverTip()
		{
			var hoverTips = new StringBuilder();
			if (isOn)
			{
				hoverTips.AppendLine($"The panel produces energy every {updateRate} seconds.");
			}
			else
			{
				hoverTips.AppendLine("It is turned off due to it being unbolted, and does not produce energy.");
			}
			if (Controller == null)
			{
				hoverTips.AppendLine("This panel is not connected to any controllers to siphon the produced energy.".Italic());
			}
			return hoverTips.ToString();
		}

		public string CustomTitle()
		{
			return $"{gameObject.ExpensiveName()} [{OnOff()}]";
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
			List<TextColor> tips = new List<TextColor>();
			tips.Add(new TextColor() { Text = "Use a wrench to bolt this panel down/up.", Color = Color.green });
			if (Controller == null) tips.Add(new TextColor(){ Text = "Use a multi-tool to connect this panel to a Solar Panel Controller.", Color = Color.green});
			return tips;
		}
	}
}