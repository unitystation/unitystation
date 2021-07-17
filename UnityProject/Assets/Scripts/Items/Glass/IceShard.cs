using System;
using System.Collections;
using System.Collections.Generic;
using Systems.Atmospherics;
using UnityEngine;

namespace Items
{
	public class IceShard : MonoBehaviour, ICheckedInteractable<HandApply>, ICheckedInteractable<InventoryApply>
	{
		[SerializeField]
		private bool isHotIce = default;

		private RegisterTile registerTile;
		private Stackable stackable;
		private Pickupable pickupable;

		private MetaDataNode metaDataNode;

		private Vector3 posCache;

		#region Lifecycle

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			stackable = GetComponent<Stackable>();
			pickupable = GetComponent<Pickupable>();
		}

		private void OnEnable()
		{
			if (CustomNetworkManager.IsServer)
			{
				UpdateManager.Add(ServerUpdateCycle, 1f);
			}
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.IsServer)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, ServerUpdateCycle);
			}
		}

		private void ServerUpdateCycle()
		{
			var pos = registerTile.WorldPosition;

			//If in an itemslot try to get that root position
			if (pickupable.ItemSlot != null)
			{
				pos = pickupable.ItemSlot.GetRootStorageOrPlayer().gameObject.WorldPosServer().RoundToInt();
			}

			//If the position is still hidden then the shard or the top pickupable is also hidden
			if (pos == TransformState.HiddenPos)
			{
				return;
			}

			//Cache pos so we dont try to get the metadata every update if we haven't moved
			if (pos != posCache)
			{
				metaDataNode = MatrixManager.GetMetaDataAt(pos);
				posCache = pos;
			}

			if (metaDataNode == null) return;

			if (isHotIce == false && metaDataNode.GasMix.Temperature > AtmosDefines.WATER_VAPOR_FREEZE)
			{
				MeltIce(metaDataNode);
			}

			if (isHotIce && metaDataNode.GasMix.Temperature > 373.15)
			{
				MeltHotIce(metaDataNode);
			}
		}

		#endregion

		private void MeltIce(MetaDataNode node)
		{
			node.GasMix.AddGas(Gas.WaterVapor, stackable.Amount * 2f);
			_ = Despawn.ServerSingle(gameObject);
		}

		private void MeltHotIce(MetaDataNode node)
		{
			if (node == null) return;

			node.GasMix.AddGas(Gas.Plasma, stackable.Amount * 150);
			node.GasMix.ChangeTemperature(stackable.Amount * 20 + 300);
			_ = Despawn.ServerSingle(gameObject);
		}

		#region Interaction

		//Used on shard on tile
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Welder)) return true;

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			OnWelderUse(interaction);
		}

		//Used on shard in inventory
		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			if (Validations.CanInteract(interaction.PerformerPlayerScript, side) == false) return false;

			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Welder)) return true;

			return false;
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			OnWelderUse(interaction);
		}

		private void OnWelderUse(Interaction interaction)
		{
			if (Validations.HasUsedActiveWelder(interaction) == false) return;

			var pos = registerTile.WorldPositionServer;

			//If in inventory use player pos instead, check root in case player is in something
			if (pickupable.ItemSlot != null)
			{
				pos = pickupable.ItemSlot.GetRootStorageOrPlayer().gameObject.WorldPosServer().RoundToInt();
			}

			//If hidden then stop
			if (pos == TransformState.HiddenPos)
			{
				return;
			}

			Chat.AddActionMsgToChat(interaction.Performer, $"You melt the {gameObject.ExpensiveName()} with the {interaction.UsedObject.ExpensiveName()}",
				$"{interaction.Performer.ExpensiveName()} melts the {gameObject.ExpensiveName()} with the {interaction.UsedObject.ExpensiveName()}");

			if (isHotIce == false)
			{
				MeltIce(MatrixManager.GetMetaDataAt(pos));
				return;
			}

			MeltHotIce(MatrixManager.GetMetaDataAt(pos));
		}

		#endregion
	}
}
