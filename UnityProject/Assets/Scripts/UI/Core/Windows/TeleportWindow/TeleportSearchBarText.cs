using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Core.Windows
{
	public class TeleportSearchBarText : MonoBehaviour
	{
		public void OnSearch()//called when search field is changed or finished being edited
		{
			gameObject.transform.parent.GetComponent<TeleportButtonSearchBar>().Search();
		}
	}
}
