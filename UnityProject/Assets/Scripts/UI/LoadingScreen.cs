using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
	[SerializeField] private Scrollbar scrollBar = null;

	/// <summary>
	/// Set between 0f to 1f
	/// </summary>
	public void SetLoadBar(float loadAmount)
	{
		scrollBar.size = loadAmount;
	}
}
