using System.Collections;
using Items;
using Messages.Server;
using UnityEngine;

namespace Weapons.ActivatableWeapons
{
	[RequireComponent(typeof(Pickupable))]
	[RequireComponent(typeof(ItemAttributesV2))]
	public class ChangeSpriteOnActivate : MonoBehaviour
	{

		protected ActivatableWeapon av;

		private void Awake()
		{
			av = GetComponent<ActivatableWeapon>();
			av.ServerOnActivate += ServerActivateBehaviour;
			av.ServerOnDeactivate += ServerDeactivateBehaviour;
			av.ClientOnActivate += ClientActivateBehaviour;
			av.ClientOnDeactivate += ClientDeactivateBehaviour;
		}

		private Pickupable pickupable;
		private ItemAttributesV2 itemAttributes;

		public SpriteHandler ItemIcon;

		public ItemsSprites ActivatedSprites = new();
		private ItemsSprites defaultSprites = new();

		private void Start()
		{
			itemAttributes = GetComponent<ItemAttributesV2>();
			pickupable = GetComponent<Pickupable>();
			defaultSprites = itemAttributes.ItemSprites;
		}

		public void ServerActivateBehaviour(GameObject performer)
		{
			itemAttributes.SetSprites(ActivatedSprites);
			ItemIcon.SetSpriteSO(ActivatedSprites.SpriteInventoryIcon);
			StartCoroutine(SendUpdateMsg(performer));
		}

		public void ServerDeactivateBehaviour(GameObject performer)
		{
			itemAttributes.SetSprites(defaultSprites);
			ItemIcon.SetSpriteSO(defaultSprites.SpriteInventoryIcon);
			StartCoroutine(SendUpdateMsg(performer));
		}

		private IEnumerator SendUpdateMsg (GameObject performer)
		{
			yield return new WaitForEndOfFrame();

			if (pickupable.ItemSlot != null)
			{
				PlayerAppearanceMessage.SendToAll(performer,
					(int) pickupable.ItemSlot.NamedSlot.GetValueOrDefault(NamedSlot.none), gameObject);
			}
		}

		public void ClientActivateBehaviour()
		{
			itemAttributes.SetSprites(ActivatedSprites);
		}

		public void ClientDeactivateBehaviour()
		{
			itemAttributes.SetSprites(defaultSprites);
		}
	}
}