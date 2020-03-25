using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportSearchBarText : MonoBehaviour
{
  public void OnSearch()
	{
		gameObject.transform.parent.GetComponent<TeleportButtonSearchBar>().Search();
	}
}
