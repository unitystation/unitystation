using UnityEngine;
using US13.UI.Systems.AdminTools;

namespace US13.UI.Systems.MainHUD.UI_Bottom
{
	public class AdminSearchBarText : MonoBehaviour
	{
		public void OnSearch()//called when search field is changed or finished being edited
		{
			gameObject.transform.parent.parent.GetComponent<AdminPlayersScrollView>().Search();
		}
	}
}
