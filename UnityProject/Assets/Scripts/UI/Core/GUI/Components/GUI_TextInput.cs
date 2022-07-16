using UnityEngine;
using UnityEngine.UI;


	public class GUI_TextInput : GUI_Component
	{
		public GameObject label;

		private InputField inputField;
		private Text inputFieldText;
		private Color initialLabelColor;
		private Color initialTextColor;
		private Text labelText;

		void Awake()
		{
			labelText = label.GetComponent<Text>();
			initialLabelColor = labelText.color;

			inputField = GetComponent<InputField>();
			inputFieldText = GetComponentInChildren<Text>();
			if (inputFieldText != null)
			{
				initialTextColor = inputFieldText.color;
			}
		}

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		void UpdateMe()
		{
			// Update child text color
			if (inputFieldText != null && initialTextColor != null)
			{
				inputFieldText.color = inputField.interactable ? initialTextColor : inputField.colors.disabledColor;
				labelText.color = inputField.interactable ? initialLabelColor : inputField.colors.disabledColor;
			}

			// Update label color
			if (initialLabelColor != null)
			{
				labelText.color = inputField.interactable ? initialLabelColor : inputField.colors.disabledColor;
			}
		}
	}

