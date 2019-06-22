using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Text;
using Newtonsoft.Json;

public class SUB_ElementHandler : MonoBehaviour
{
	public GameObject DynamicSizePanel;

	public GUI_sub_P_Bool boolP;
	public GUI_sub_P_Input InputP;
	public GUI_sub_P_Class ClassP;
	public GUI_sub_P_Collection CollectionP;
	public GUI_sub_P_list_Dict P_list_Dict;


	public VariableViewerNetworking.NetFriendlySentence Sentence;

	public void ValueSetUp()
	{
		ValueSetUpKeyOrValue(true);
		if (Sentence.KeyVariableType != null) { 
			ValueSetUpKeyOrValue(false);
		}
	}
	public void ValueSetUpKeyOrValue(bool isValue ) {
		Type t;
		string Value;
		if (isValue)
		{
			t = Librarian.UEGetType(Sentence.ValueVariableType);
			Value = Sentence.ValueVariable;
		}
		else { 
			t = Librarian.UEGetType(Sentence.KeyVariableType);
			Value = Sentence.KeyVariable;
		}

		bool pass = false;
		if (t != null)
		{

			//Logger.Log(t.ToString());
			if (t == typeof(bool))
			{
				
		
				GUI_sub_P_Bool ValueEntry = Instantiate(boolP) as GUI_sub_P_Bool;
				ValueEntry.transform.SetParent(DynamicSizePanel.transform);
				ValueEntry.transform.localScale = Vector3.one;
				ValueEntry.TToggle.isOn = bool.Parse(Value);
				pass = true;
			}
			else if (t.IsGenericType)
			{

	
				GUI_sub_P_list_Dict ValueEntry = Instantiate(P_list_Dict) as GUI_sub_P_list_Dict;

				ValueEntry.transform.SetParent(DynamicSizePanel.transform);
				ValueEntry.transform.localScale = Vector3.one;
				ValueEntry.TText.text = Value;
				ValueEntry.ID = Sentence.SentenceID;
				ValueEntry.Sentence = Sentence;
				pass = true;
			}
			else if (!t.IsValueType)
			{
				
		
				GUI_sub_P_Class ValueEntry = Instantiate(ClassP) as GUI_sub_P_Class;
				ValueEntry.transform.SetParent(DynamicSizePanel.transform);
				ValueEntry.transform.localScale = Vector3.one;
				ValueEntry.TText.text = Value;
				ValueEntry.ID = Sentence.SentenceID;
				pass = true;

			}
			else if (t.IsEnum)
			{
				//IsEnum!!

		
				GUI_sub_P_Collection ValueEntry = Instantiate(CollectionP) as GUI_sub_P_Collection;
		
			
				ValueEntry.transform.SetParent(DynamicSizePanel.transform);
				ValueEntry.transform.localScale = Vector3.one;
				ValueEntry.TDropdown.ClearOptions();
				ValueEntry.TDropdown.captionText.text = Value;
				var values = Enum.GetValues(t);
				List<string> llist = new List<string>();
				int Count = 0;
				int Selected = 0;
				foreach (var st in values)
				{
					if (st.ToString() == Value)
					{
						Selected = Count;
					}
					llist.Add(st.ToString());
					Count++;
				}
				ValueEntry.TDropdown.AddOptions(llist);
				ValueEntry.TDropdown.value = Selected;

				pass = true;
			}

		}

		if (!pass)
		{
			GUI_sub_P_Input ValueEntry = Instantiate(InputP) as GUI_sub_P_Input;
			ValueEntry.transform.SetParent(DynamicSizePanel.transform);
			ValueEntry.transform.localScale = Vector3.one;
			ValueEntry.TInputField.text = Value;
		}
	}
}
