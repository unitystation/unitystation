using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DragAndDrop : MonoBehaviour
{
	public Image dragDummy;

	public void Start()
	{
		dragDummy.enabled = false;
	}
	public void StartDrag(GameObject item)
	{
		Debug.Log("Start Draggin");
	}
}