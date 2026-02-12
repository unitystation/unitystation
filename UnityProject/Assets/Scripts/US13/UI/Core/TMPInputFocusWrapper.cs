using TMPro;
using UnityEngine;
using US13.UI.Systems;

namespace US13.UI.Core
{
	public class TMPInputFocusWrapper : MonoBehaviour
	{

		private TMP_InputField Field;

		public void Awake()
		{
			Field = this.GetComponent<TMP_InputField>();
			Field.onSelect.AddListener(Focused);
			Field.onDeselect.AddListener(UnFocused);
		}

		public void Focused(string data)
		{
			UIManager.IsInputFocus = true;
			UIManager.PreventChatInput = true;
		}

		public void UnFocused(string data)
		{
			UIManager.IsInputFocus = false;
			UIManager.PreventChatInput = false;
		}
		public void OnDisable()
		{
			UnFocused("");
		}

		public void OnDestroy()
		{
			UnFocused("");
		}
	}
}
