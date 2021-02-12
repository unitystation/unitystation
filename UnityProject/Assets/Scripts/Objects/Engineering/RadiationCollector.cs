using System;
using UnityEngine;

namespace Objects.Engineering
{
	public class RadiationCollector : MonoBehaviour, ICheckedInteractable<HandApply>, IExaminable
	{
		private ElectricalNodeControl electricalNodeControl;
		private ModuleSupplyingDevice moduleSupplyingDevice;
		private PushPull pushPull;
		private RegisterTile registerTile;

		private bool isOn;
		private bool isWrenched;
		private GameObject slotObject;

		[SerializeField]
		private SpriteHandler mainSpriteHandler = null;
		[SerializeField]
		private SpriteHandler slotHandler = null;

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
			pushPull = GetComponent<PushPull>();
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
			if(CustomNetworkManager.IsServer == false) return;

			if (startSetUp)
			{
				isWrenched = true;
				pushPull.ServerSetPushable(false);
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

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench)) return true;

			if (interaction.HandObject == null) return true;

			//TODO adding/removing tank logic

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
			{
				TryWrench(interaction);
			}
			else if (interaction.HandObject == null)
			{
				SupplyToggle(interaction);
			}
		}

		private void TryWrench(HandApply interaction)
		{
			if (isOn)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Turn off the collector first");
				return;
			}

			if (isWrenched)
			{
				//unwrench
				ToolUtils.ServerUseToolWithActionMessages(interaction, 1,
					"You start to wrench the radiation collector...",
					$"{interaction.Performer.ExpensiveName()} starts to wrench the radiation collector...",
					"You wrench the radiation collector off the floor.",
					$"{interaction.Performer.ExpensiveName()} wrenches the radiation collector off the floor.",
					() =>
					{
						isWrenched = false;
						pushPull.ServerSetPushable(true);
						ChangePowerState(false);
					});
			}
			else
			{
				if (!registerTile.Matrix.MetaTileMap.HasTile(registerTile.WorldPositionServer, LayerType.Base))
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, "Radiation collector needs to be on a base floor");
					return;
				}

				//wrench
				ToolUtils.ServerUseToolWithActionMessages(interaction, 1,
					"You start to wrench the radiation collector...",
					$"{interaction.Performer.ExpensiveName()} starts to wrench the radiation collector...",
					"You wrench the radiation collector onto the floor.",
					$"{interaction.Performer.ExpensiveName()} wrenches the radiation collector onto the floor.",
					() =>
					{
						isWrenched = true;
						pushPull.ServerSetPushable(false);
					});
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
			else if (isWrenched)
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

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			if (isOn == false)
			{
				return "Collector is off";
			}

			return $"Radiation level is {radiationLevel}\nGenerating {generatedWatts} watts of energy";
		}
	}
}
