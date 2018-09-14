using UnityEngine;
using UnityEngine.UI;

public class PlayerListUI : MonoBehaviour
{
	public Text nameList;
	public GameObject window;

	public ScrollRect scrollRect;

	void OnEnable()
	{
		Invoke("SetScrollToTop",0.1f);
	}

	void SetScrollToTop()
	{
		scrollRect.verticalScrollbar.value = 1f;
	}
}