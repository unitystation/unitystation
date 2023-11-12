using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Systems.Faith.UI
{
	public class FaithPresetButton : MonoBehaviour
	{
		[SerializeField] private TMP_Text buttonName;
		[SerializeField] private Image icon;
		public Faith Faith { get; private set; }
		private ChaplainFirstTimeSelectScreen father;

		public void Setup(Faith faith, ChaplainFirstTimeSelectScreen screen)
		{
			Faith = faith;
			buttonName.text = faith.FaithName;
			icon.sprite = faith.FaithIcon;
			father = screen;
		}

		public void OnButtonClick()
		{
			father.SetFaith(Faith);
		}
	}
}