using UnityEngine;
using UnityEngine.UI;

namespace US13.UI.Core
{
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
}
