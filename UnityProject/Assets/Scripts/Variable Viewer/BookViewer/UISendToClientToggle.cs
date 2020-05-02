using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISendToClientToggle : MonoBehaviour
{
	public static bool toggle = false;

	public Image image;
	public void Toggle()
	{
		toggle = !toggle;
		image.enabled = toggle;
	}
}
