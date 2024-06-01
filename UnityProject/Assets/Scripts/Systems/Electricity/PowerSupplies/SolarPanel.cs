using System.Linq;
using AddressableReferences;
using Core;
using Messages.Server.SoundMessages;
using Mirror;
using Shared.Systems.ObjectConnection;
using UnityEngine;

namespace Systems.Electricity.PowerSupplies
{
	[RequireComponent(typeof(UniversalObjectPhysics))]
	public class SolarPanel : NetworkBehaviour, ICheckedInteractable<HandApply>, IMultitoolSlaveable, IExaminable
	{
		private const int MAXIMUM_ALLOWED_CLUTTER = 8;

		[field: SerializeField] public UniversalObjectPhysics Physics { get; private set; }
		[field: SerializeField] public int LastProducedWatts { get; private set; } = 0;
		[SerializeField] private float updateRate = 8f;
		[SerializeField] private int productionPowerPointsPerFreeAvaliableSide = 150;
		[SerializeField] private bool isOn = true;
		[SerializeField] private AddressableAudioSource warningBeeper;

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
			Controller = controller;
			return controller.AddDevice(this);
		}

		public void SetMasterEditor(IMultitoolMasterable master)
		{
			if (master.gameObject.TryGetComponent<SolarPanelController>(out var controller) == false) return;
			Master = master;
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
		}

		private void OnDestroy()
		{
			if (CustomNetworkManager.IsServer) UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			if (isOn == false || Controller == null) return;
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

			LastProducedWatts = points * 2;
			if (points == 0)
			{
				if (warningBeeper != null) SoundManager.PlayNetworkedAtPos(warningBeeper, gameObject.AssumedWorldPosServer(), beeperSettings);
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (interaction.Intent != Intent.Help) return false;
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.HandObject == null) return;
			var traits = interaction.HandSlot.ItemAttributes.GetTraits();
			if (traits.Contains(CommonTraits.Instance.Wrench))
			{
				Physics.SetIsNotPushable(!Physics.isNotPushable);
				UpdatePushableStateProperties();
			}
		}

		private void UpdatePushableStateProperties()
		{
			isOn = Physics.isNotPushable;
		}

		private string OnOff()
		{
			return isOn ? "on" : "off";
		}

		public string Examine(Vector3 worldPos = default)
		{
			return $"This panel is currently {OnOff()}.\n It's screen reads out: {LastProducedWatts}.";
		}
	}
}