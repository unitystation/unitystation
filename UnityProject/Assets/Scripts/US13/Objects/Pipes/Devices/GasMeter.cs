using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Core.Sprite_Handler;
using US13.Managers.MatrixManager;
using US13.Managers.UpdateManager;
using US13.Systems.Fluids;
using US13.Tilemaps.Behaviours.Meta;
using US13.Tilemaps.Behaviours.Objects;

namespace US13.Objects.Pipes.Devices
{
	public class GasMeter : MonoBehaviour, ICheckedInteractable<HandApply>, IExaminable, ICheckedInteractable<AiActivate>, IServerSpawn
	{
		[SerializeField]
		private SpriteHandler spriteHandler;

		private MetaDataNode metaDataNode;
		private RegisterTile registerTile;
		private MixAndVolume MixAndVolume;

		#region Lifecycle

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			UpdateManager.Add(CycleUpdate, 1);
		}

		public void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CycleUpdate);
		}

		#endregion

		public string Examine(Vector3 worldPos = default)
		{
			return ReadMeter();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.TargetObject != gameObject) return false;
			if (interaction.HandObject != null) return false;
			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, ReadMeter());
		}

		public bool WillInteract(AiActivate interaction, NetworkSide side)
		{
			//Only normal click
			if (interaction.ClickType != AiActivate.ClickTypes.NormalClick) return false;

			if (DefaultWillInteract.AiActivate(interaction, side) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(AiActivate interaction)
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, ReadMeter());
		}

		private string ReadMeter()
		{
			if (metaDataNode?.PipeData?.Count > 0)
			{
				var gasInfo = metaDataNode.PipeData[0].pipeData.GetMixAndVolume;
				string pressure = gasInfo.Density().y.ToString("#.00");
				string tempK = gasInfo.Temperature.ToString("#.00");
				string tempC = (gasInfo.Temperature - 273.15f).ToString("#.00");
				return $"The pressure gauge reads {pressure} kPa, with a temperature of {tempK} K ({tempC} °C).";
			}
			else
			{
				return "The meter is not connected to anything.";
			}
		}

		public void CycleUpdate()
		{
			if (metaDataNode == null)
			{
				metaDataNode = MatrixManager.AtPoint(registerTile.WorldPositionServer, true).MetaDataLayer
					.Get(registerTile.LocalPositionServer, false);
			}

			if (metaDataNode.PipeData.Count > 0)
			{
				MixAndVolume = metaDataNode.PipeData[0].pipeData.GetMixAndVolume;
				if (MixAndVolume.Density().y == 0)
				{
					spriteHandler.SetCatalogueIndexSprite(0);
				}
				else
				{
					int toSet = (int)Mathf.Floor(MixAndVolume.Density().y / (500f)); //10000f/20f
					if (toSet == 0)
					{
						toSet = 1;
					}

					if (toSet > 20)
					{
						toSet = 20;
					}

					spriteHandler.SetCatalogueIndexSprite(toSet);
				}
			}
			else
			{
				spriteHandler.SetCatalogueIndexSprite(0);
			}
		}
	}
}
