using UnityEngine;
using US13.UI.Systems.AdminTools;

namespace US13.UI.Systems.MainHUD.UI_Bottom
{
	public class AdminMindSearchBarText : MonoBehaviour
	{
		public void OnSearch()//called when search field is changed or finished being edited
		{
			gameObject.GetComponentInParent<AdminMindScrollView>().Search();
		}
	}
}
