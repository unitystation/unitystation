using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdminGlobalSoundButtonTab : MonoBehaviour
{

	public void OnClick()
	{
		var controller = gameObject.transform.parent.parent.parent.Find("GlobalSoundButtonScrollList").gameObject;

		if (!controller.activeInHierarchy)
		{
			controller.SetActive(true);
		}
		else
		{
			controller.SetActive(false);
		}
	}
}
