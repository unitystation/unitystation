using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Messages.Client.VariableViewer;
using SecureStuff;


namespace AdminTools.VariableViewer
{
	public class GUI_PageEntry : MonoBehaviour
	{
		public Image TypeImage;

		public TMP_Text VariableName;

		public GameObject DynamicSizePanel;

		public GameObject FunctionButton;

		public bool NotPoolble;

		private VariableViewerNetworking.NetFriendlyPage _Page;
		public VariableViewerNetworking.NetFriendlyPage Page {
			get { return _Page; }
			set {
				VariableName.text = value.VariableName;
				//Variable.text = value.Variable;
				//VariableType.text = " VariableType > " + value.VariableType;
				_Page = value;
				ValueSetUp();
			}
		}

		public void ValueSetUp()
		{
			if (Page.CanWrite)
			{
				switch (Page.VVHighlight)
				{
					case VVHighlight.None:
						TypeImage.color = Color.gray;
						break;
					case VVHighlight.SafeToModify:
						TypeImage.color = Color.cyan;
						break;
					case VVHighlight.SafeToModify100:
						TypeImage.color = Color.green;
						break;
					case VVHighlight.UnsafeToModify:
						TypeImage.color = new Color(1, 0.498039f, 0);
						break;
					case VVHighlight.VariableChangeUpdate:
						TypeImage.color = Color.yellow;
						break;
					case VVHighlight.DEBUG:
						TypeImage.color = Color.red;
						break;
				}

			}
			else
			{
				TypeImage.color = Color.blue;
			}

			//Activate function

			//FunctionButton
			if (Page.VariableType == null)
			{
				FunctionButton.gameObject.SetActive(true);
			}
			else
			{
				FunctionButton.gameObject.SetActive(false);
			}

			VVUIElementHandler.ProcessElement(DynamicSizePanel, _Page);
		}

		public void InvokeMethod()
		{
			RequestInvokeFunction.Send(_Page.ID, UISendToClientToggle.toggle);
		}
	}
}
