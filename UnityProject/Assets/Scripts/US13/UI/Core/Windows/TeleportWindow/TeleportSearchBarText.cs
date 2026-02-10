using UnityEngine;

namespace US13.UI.Core.Windows.TeleportWindow
{
	public class TeleportSearchBarText : MonoBehaviour
	{
		public void OnSearch()//called when search field is changed or finished being edited
		{
			gameObject.transform.parent.GetComponent<TeleportButtonSearchBar>().Search();
		}
	}
}
