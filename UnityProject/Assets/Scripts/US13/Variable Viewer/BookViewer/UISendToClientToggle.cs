using UnityEngine;
using UnityEngine.UI;

namespace US13.Variable_Viewer.BookViewer
{
	public class UISendToClientToggle : MonoBehaviour
	{
		public static bool toggle = false;

		public Image image;
		public void Toggle()
		{
			toggle = !toggle;
			image.enabled = toggle;
		}
	}
}
