using UnityEngine;

namespace Objects.Medical
{
	public class BodyBag : MonoBehaviour, ICheckedInteractable<MouseDrop>, IServerSpawn, IRightClickable
	{
		public GameObject prefabVariant;

		public void OnSpawnServer(SpawnInfo info)
		{
			GetComponent<ClosetControl>().ServerToggleClosed(false);
		}

		public bool WillInteract(MouseDrop interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side))
			{
				return false;
			}

			var cnt = GetComponent<CustomNetTransform>();
			var ps = interaction.Performer.GetComponent<PlayerScript>();

			var pna = interaction.Performer.GetComponent<PlayerNetworkActions>();

			if (!pna
				|| interaction.Performer != interaction.TargetObject
				|| interaction.DroppedObject != gameObject
				|| pna.GetActiveHandItem() != null
				|| !ps.IsRegisterTileReachable(cnt.RegisterTile, side == NetworkSide.Server))
			{
				return false;
			}

			return true;
		}

		public void ServerPerformInteraction(MouseDrop interaction)
		{
			var pna = interaction.Performer.GetComponent<PlayerNetworkActions>();

			var closetControl = GetComponent<ClosetControl>();
			if (!closetControl.IsClosed)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer,
					"You wrestle with the body bag, but it won't fold while unzipped.");
				return;
			}

			if (!closetControl.ServerIsEmpty())
			{
				Chat.AddExamineMsgFromServer(interaction.Performer,
					"There are too many things inside of the body bag to fold it up!");
				return;
			}

			// Add folded to player inventory (note, this is actually a new object, not this object)
			//TODO: This means that body bag integrity gets reset every time it is picked up. Should be converted to be the same object instead.
			var folded = Spawn.ServerPrefab(prefabVariant).GameObject;
			Inventory.ServerAdd(folded,
				interaction.Performer.GetComponent<ItemStorage>().GetActiveHandSlot());
			// Remove from world
			Despawn.ServerSingle(gameObject);
		}

		public RightClickableResult GenerateRightClickOptions()
		{
			var result = RightClickableResult.Create();

			if (WillInteract(MouseDrop.ByLocalPlayer(gameObject, PlayerManager.LocalPlayer), NetworkSide.Client))
			{
				result.AddElement("Fold Up", RightClickInteract);
			}

			return result;
		}

		private void RightClickInteract()
		{
			InteractionUtils.RequestInteract(MouseDrop.ByLocalPlayer(gameObject, PlayerManager.LocalPlayer), this);
		}
	}
}
