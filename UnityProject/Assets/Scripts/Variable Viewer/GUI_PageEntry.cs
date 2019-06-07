using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Text;


public class GUI_PageEntry : MonoBehaviour
{

	public Text PageID;
	public Text VariableName;
	public InputField Variable;
	//public Text VariableType;
	public GUI_P_Bool boolP;
	public GUI_P_Input InputP;
	public GUI_P_Class ClassP;
	public GUI_P_Collection CollectionP;

	public GameObject DynamicSizePanel;

	public bool IsSetUp;

	private BookNetMessage.NetFriendlyPage _Page;
	public BookNetMessage.NetFriendlyPage Page
	{
		get { return _Page; }
		set
		{
			PageID.text = "ID > " + value.ID;
			VariableName.text = value.VariableName;
			//Variable.text = value.Variable;
			//VariableType.text = " VariableType > " + value.VariableType;
			_Page = value;
			ValueSetUp();
		}
	}
	public void ValueSetUp() {
		if (!IsSetUp) { 
			Type t = Librarian.UEGetType(_Page.VariableType);
			bool pass = false;
			if (t == typeof(bool))
			{
				GUI_P_Bool ValueEntry = Instantiate(boolP) as GUI_P_Bool;
				ValueEntry.transform.SetParent(DynamicSizePanel.transform);
				ValueEntry.transform.localScale = Vector3.one;
				ValueEntry.TToggle.isOn = bool.Parse(_Page.Variable);
				pass = true;

			}
			else if (t != null) {
				if (t.IsClass)
				{
					GUI_P_Class ValueEntry = Instantiate(ClassP) as GUI_P_Class;
					ValueEntry.transform.SetParent(DynamicSizePanel.transform);
					ValueEntry.transform.localScale = Vector3.one;
					ValueEntry.TText.text = _Page.Variable;
					ValueEntry.ID = _Page.ID;
					pass = true;
				}
			
			}

			if (!pass)
			{
				GUI_P_Input ValueEntry = Instantiate(InputP) as GUI_P_Input;
				ValueEntry.transform.SetParent(DynamicSizePanel.transform);
				ValueEntry.transform.localScale = Vector3.one;
				ValueEntry.TInputField.text = _Page.Variable;
			}


	
		}
	}
}
