using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportCloseButton : MonoBehaviour
{
    public void OnClick()
    {
		transform.parent.gameObject.SetActive(false);
		transform.parent.gameObject.transform.parent.gameObject.GetComponent<UI_GhostOptions>().TeleportScreenStatus();
	}
}
