using System.Collections.Generic;
using System.Linq;
using AddressableReferences;
using Core;
using Messages.Server.SoundMessages;
using Mirror;
using Shared.Systems.ObjectConnection;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine;
using Util.Independent.FluentRichText;

namespace Systems.Electricity.PowerSupplies
{
	[RequireComponent(typeof(UniversalObjectPhysics))]
	public class SolarPanel : NetworkBehaviour, ICheckedInteractable<HandApply>, IMultitoolSlaveable, IExaminable, IHoverTooltip
	{
		[field: SerializeField] public UniversalObjectPhysics Physics { get; private set; }
		[field: SerializeField] public int LastProducedWatts { get; private set; } = 0;
		[SerializeField] private float updateRate = 16f;
		[SerializeField] private int productionPowerPointsPerFreeAvaliableSide = 150;
		[SerializeField] private bool isOn = true;
		[SerializeField] private AddressableAudioSource warningBeeper;
		[SerializeField] private AddressableAudioSource wrenchSound;

		private AudioSourceParameters beeperSettings = new AudioSourceParameters()
		{
			Volume = 0.5f,
		};

		public MultitoolConnectionType ConType { get; } = MultitoolConnectionType.SolarPanel;
		public bool CanRelink { get; } = true;
		public bool RequireLink { get; } = true;
		public IMultitoolMasterable Master { get; set; }
		public SolarPanelController Controller;

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

		private void Awake()
		{
			Physics ??= GetComponent<UniversalObjectPhysics>();
			if (CustomNetworkManager.IsServer) UpdateManager.Add(UpdateMe, updateRate);
			if (Controller != null)
			{
				Controller.AddDevice(this);
				Master ??= Controller;
			}
			else
			{
				isOn = false;
			}
		}

		private void OnDestroy()
		{
			if (CustomNetworkManager.IsServer) UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			if (isOn == false) return;
			var points = CalculatePoints();
			LastProducedWatts = points * 2;
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
			return Examine();
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
			List<TextColor> tips = new List<TextColor>();
			if (isOn)
			{
				tips.Add(new TextColor() { Text = $"The panel produces energy every {updateRate} seconds.".Italic(), Color = Color.grey });
			}
			else
			{
				tips.Add(new TextColor() { Text = "An unbolted panel is turned off, and does not produce energy.".Italic(), Color = Color.grey });
			}
			tips.Add(new TextColor() { Text = "Use a wrench to bolt this panel down/up.", Color = Color.green });
			if (Controller == null) tips.Add(new TextColor(){ Text = "Use a multi-tool to connect this panel to a Solar Panel Controller.", Color = Color.green});
			return tips;
		}
	}
}