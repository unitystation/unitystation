using UnityEngine;

public class AdminMindSearchBarText : MonoBehaviour
{
	public void OnSearch()//called when search field is changed or finished being edited
	{
		gameObject.GetComponentInParent<AdminMindScrollView>().Search();
	}
}
