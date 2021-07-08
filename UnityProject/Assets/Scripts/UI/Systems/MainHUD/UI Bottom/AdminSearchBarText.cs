using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;

public class AdminSearchBarText : MonoBehaviour
{
	public void OnSearch()//called when search field is changed or finished being edited
	{
		gameObject.transform.parent.parent.GetComponent<AdminPlayersScrollView>().Search();
	}
}
