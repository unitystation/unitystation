using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerLightHandler : NetworkBehaviour {
	private GameObject PlayerLight;

	void Start() {
		OffLight();
	}

	private void OnLight() {
		PlayerLight.SetActive(true);
	}

	private void OffLight() {
		PlayerLight.SetActive(false);
	}

	public void CheckLight() {
		if (!isServer) {
			var childSlots = GetComponentsInChildren<UI_ItemSlot> ();

			if (UIManager.Hands.CurrentSlot.Item != null) {
				Light currentLight = UIManager.Hands.CurrentSlot.Item.gameObject.GetComponent<Light> ();
				if (currentLight != null) {
					OnLight ();
				}
			} else if (UIManager.Hands.OtherSlot.Item != null) {
				Light otherLight = UIManager.Hands.OtherSlot.Item.gameObject.GetComponent<Light> ();
				if (otherLight != null) {
					OnLight ();
				}
			} else
				foreach (UI_ItemSlot slot in InventorySlotCache.InventorySlots) {
					if (slot.Item != null) {
						Light slotLight = slot.Item.gameObject.GetComponent<Light> ();
						if (slotLight != null) {
							OnLight ();
						}
					}
				}

			OffLight ();
		} 
		else {
			OffLight ();
		}
	}
}
