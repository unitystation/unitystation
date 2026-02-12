using UnityEngine;
using UnityEngine.UI;
using US13.Core.Input_System;
using US13.Managers.UpdateManager;

namespace US13.UI.Core.GUI.Components
{
	///<Summary>
	///Use this component to enabled tabbing between input fields
	///</Summary>
	[RequireComponent(typeof(InputField))]
	public class GUI_TabNext : GUI_Component
	{
		private InputField thisField;
		public InputField nextField;

		void Start()
		{
			thisField = GetComponent<InputField>();
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
			if (CommonInput.GetKeyDown(KeyCode.Tab))
			{
				if (thisField.isFocused)
				{
					nextField.ActivateInputField();
				}
			}
		}
	}
}