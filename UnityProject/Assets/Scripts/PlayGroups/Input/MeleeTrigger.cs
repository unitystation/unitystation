using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Do not derive from NetworkBehaviour, this is also used on tilemap layers
public class MeleeTrigger : MonoBehaviour
{
	public virtual bool MeleeInteract(GameObject originator, string hand)
	{
		if (UIManager.Hands.CurrentSlot.Item != null)
		{
			var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			var handItem = UIManager.Hands.CurrentSlot.Item.GetComponent<ItemAttributes>();
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
				PlayerManager.LocalPlayerScript.IsInReach(mousePos))
			{
				if (UIManager.CurrentIntent == Intent.Attack ||
					handItem.itemType != ItemType.Gun ||
					handItem.itemType != ItemType.Knife ||
					handItem.itemType != ItemType.Belt)
				{
					Vector2 dir = (mousePos - PlayerManager.LocalPlayer.transform.position).normalized;

					PlayerScript lps = PlayerManager.LocalPlayerScript;
					lps.weaponNetworkActions.CmdRequestMeleeAttack(gameObject, UIManager.Hands.CurrentSlot.eventName, dir,
						UIManager.DamageZone);
					return true;
				}
			}
		}
		return false;
	}
}