using UnityEngine;

namespace US13.UI.Systems.AdminTools
{
	public class AdminGlobalAudioSearchText : MonoBehaviour
	{
		public void OnSearch() //called when search field is changed or finished being edited
		{
			gameObject.transform.parent.GetComponent<AdminGlobalAudioSearchBar>().Search();
		}
	}
}
