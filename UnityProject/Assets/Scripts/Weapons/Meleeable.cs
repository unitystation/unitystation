using UnityEngine;

//Do not derive from NetworkBehaviour, this is also used on tilemap layers
/// <summary>
/// Allows an object to be attacked by melee. Supports being placed on tilemap layers for meleeing tiles
/// </summary>
public class Meleeable : MonoBehaviour, IInteractable<PositionalHandApply>
{
	//Cache these on start for checking at runtime
	private Layer tileMapLayer;
	private GameObject gameObjectRoot;

	private void Start()
	{
		gameObjectRoot = transform.root.gameObject;

		var layer = gameObject.GetComponent<Layer>();
		if (layer != null)
		{
			//this is on a tilemap:
			tileMapLayer = layer;
		}
	}

	public bool Interact(PositionalHandApply interaction)
	{
		//meleeable is only checked on the target of a melee interaction
		if (interaction.UsedObject == gameObject) return false;

		if (interaction.HandObject != null)
		{
			var handItem = interaction.HandObject.GetComponent<ItemAttributes>();

			if (handItem.itemType == ItemType.Food || handItem.itemType == ItemType.Medical) {
				return false;
			}

			//special case
			//We don't melee if we are wielding a gun with ammo and clicking ourselves (we will instead shoot ourselves)
			if (interaction.TargetObject == interaction.Performer)
			{
				var gun = handItem.GetComponent<Gun>();
				if (gun != null)
				{
					if (gun.CurrentMagazine != null && gun.CurrentMagazine.ammoRemains > 0)
					{
						//we have ammo and are clicking ourselves - don't melee. Shoot instead.
						return false;
					}
				}
			}

			if (handItem.itemType != ItemType.ID &&
				handItem.itemType != ItemType.Back &&
				handItem.itemType != ItemType.Ear &&
				handItem.itemType != ItemType.Food &&
				handItem.itemType != ItemType.Glasses &&
				handItem.itemType != ItemType.Gloves &&
				handItem.itemType != ItemType.Hat &&
				handItem.itemType != ItemType.Mask &&
				handItem.itemType != ItemType.Neck &&
				handItem.itemType != ItemType.Shoes &&
				handItem.itemType != ItemType.Suit &&
				handItem.itemType != ItemType.Uniform &&
				PlayerManager.LocalPlayerScript.IsInReach(interaction.WorldPositionTarget, false))
			{
				if (UIManager.CurrentIntent == Intent.Harm ||
					handItem.itemType != ItemType.Gun ||
					handItem.itemType != ItemType.Knife ||
					handItem.itemType != ItemType.Belt)
				{
					Vector2 dir = ((Vector3)interaction.WorldPositionTarget - PlayerManager.LocalPlayer.transform.position).normalized;

					//special case - when we have a gun and click ourselves, we should actually shoot ourselves rather than melee, which is handled elsewhere
					if (handItem.itemType == ItemType.Gun && interaction.Performer == gameObject)
					{
						return false;
					}

					PlayerScript lps = PlayerManager.LocalPlayerScript;

					if (tileMapLayer == null)
					{
						lps.weaponNetworkActions.CmdRequestMeleeAttack(gameObject, UIManager.Hands.CurrentSlot.eventName, dir,
							UIManager.DamageZone, LayerType.None);
					}
					else
					{
						lps.weaponNetworkActions.CmdRequestMeleeAttack(gameObjectRoot, UIManager.Hands.CurrentSlot.eventName, dir,
							UIManager.DamageZone, tileMapLayer.LayerType);
					}
					return true;
				}
			}
		}
		return false;

	}
}