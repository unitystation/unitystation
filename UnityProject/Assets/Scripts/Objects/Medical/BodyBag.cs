using UnityEngine;

namespace Objects.Medical
{
	public class BodyBag : MonoBehaviour, ICheckedInteractable<MouseDrop>, IServerSpawn, IRightClickable
	{
		[SerializeField]
		private GameObject prefabVariant;

		private RegisterObject registerObject;
		private ObjectContainer container;
		private ClosetControl closet;

		public void OnSpawnServer(SpawnInfo info)
		{
			registerObject = GetComponent<RegisterObject>();
			container = GetComponent<ObjectContainer>();
			closet = GetComponent<ClosetControl>();
			closet.SetDoor(ClosetControl.Door.Opened);
		}

		public bool WillInteract(MouseDrop interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false)
			{
				return false;
			}

			var ps = interaction.PerformerPlayerScript;
			var pna = ps.playerNetworkActions;

			if (!pna
				|| interaction.Performer != interaction.TargetObject
				|| interaction.DroppedObject != gameObject
				|| pna.GetActiveHandItem() != null
				|| !ps.IsRegisterTileReachable(registerObject, side == NetworkSide.Server))
			{
				return false;
			}

			return true;
		}

		public void ServerPerformInteraction(MouseDrop interaction)
		{
			if (closet.IsOpen)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer,
					"You wrestle with the body bag, but it won't fold while unzipped.");
				return;
			}

			if (container.IsEmpty == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer,
					"There are too many things inside of the body bag to fold it up!");
				return;
			}

			// Add folded to player inventory (note, this is actually a new object, not this object)
			//TODO: This means that body bag integrity gets reset every time it is picked up. Should be converted to be the same object instead.
			var folded = Spawn.ServerPrefab(prefabVariant).GameObject;
			Inventory.ServerAdd(folded,
				interaction.PerformerPlayerScript.DynamicItemStorage.GetActiveHandSlot());
			// Remove from world
			_ = Despawn.ServerSingle(gameObject);
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
