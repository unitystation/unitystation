using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Sprite_Handler;
using US13.Managers.NetworkManagement;
using US13.Managers.UpdateManager;
using US13.Systems.Construction;
using US13.Systems.Electricity.NodeModules;
using US13.Tilemaps.Behaviours.Meta;
using US13.Tilemaps.Behaviours.Objects;
using Util;

namespace US13.Objects.Engineering
{
	public class RadiationCollector : MonoBehaviour, ICheckedInteractable<HandApply>, IExaminable
	{
		private ElectricalNodeControl electricalNodeControl;
		private ModuleSupplyingDevice moduleSupplyingDevice;
		private WrenchSecurable wrenchSecurable;
		private RegisterTile registerTile;

		private bool isOn;

		[SerializeField]
		private SpriteHandler mainSpriteHandler = null;

		[SerializeField]
		[Tooltip("Whether this radiation collector should start wrenched")]
		private bool startSetUp;

		[SerializeField]
		[Tooltip("radiationWatts * radiation tile level = watts this device will supply")]
		private float radiationWatts = 10f;

		private float generatedWatts = 0f;
		private float radiationLevel = 0f;

		#region LifeCycle

		private void Awake()
		{
			electricalNodeControl = GetComponent<ElectricalNodeControl>();
			moduleSupplyingDevice = GetComponent<ModuleSupplyingDevice>();
			wrenchSecurable = GetComponent<WrenchSecurable>();
			registerTile = GetComponent<RegisterTile>();
		}

		private void OnEnable()
		{
			UpdateManager.Add(CollectorUpdate, 1f);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CollectorUpdate);
		}

		private void Start()
		{
			if (CustomNetworkManager.IsServer == false) return;

			if (startSetUp)
			{
				wrenchSecurable.ServerSetPushable(false);
			}
		}

		#endregion

		private void CollectorUpdate()
		{
			if(CustomNetworkManager.IsServer == false) return;

			if (isOn == false)
			{
				moduleSupplyingDevice.ProducingWatts = 0;
				return;
			}

			MetaDataNode node = registerTile.Matrix.GetMetaDataNode(registerTile.LocalPositionServer);
			if (node == null)
			{
				radiationLevel = 0;
				moduleSupplyingDevice.ProducingWatts = 0;
				return;
			}

			radiationLevel = node.RadiationNode.RadiationLevel;
			generatedWatts = radiationLevel * radiationWatts;

			moduleSupplyingDevice.ProducingWatts = generatedWatts;
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (interaction.TargetObject != gameObject) return false;

			if (interaction.HandObject == null) return true;

			//TODO adding/removing tank logic

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.HandObject == null)
			{
				SupplyToggle(interaction);
			}
		}

		private void SupplyToggle(HandApply interaction)
		{
			if (isOn)
			{
				ChangePowerState(false);
				mainSpriteHandler.AnimateOnce(3);
				Chat.AddActionMsgToChat(interaction.Performer, "You turn off the radiation collector",
					$"{interaction.Performer.ExpensiveName()} turns off the radiation collector");
			}
			else if (wrenchSecurable.IsAnchored)
			{
				ChangePowerState(true);
				mainSpriteHandler.AnimateOnce(1);
				Chat.AddActionMsgToChat(interaction.Performer, "You turn on the radiation collector",
					$"{interaction.Performer.ExpensiveName()} turns on the radiation collector");
			}
			else
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "The radiation collector needs to be wrenched");
			}
		}

		private void ChangePowerState(bool newState)
		{
			if (newState)
			{
				isOn = true;
				electricalNodeControl.TurnOnSupply();
			}
			else
			{
				isOn = false;
				electricalNodeControl.TurnOffSupply();
			}
		}

		public string Examine(Vector3 worldPos = default)
		{
			if (isOn == false)
			{
				return "Collector is off";
			}

			return $"Radiation level is {radiationLevel}\nGenerating {generatedWatts} watts of energy";
		}
	}
}
