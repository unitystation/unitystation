using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Text;
using Newtonsoft.Json;

public class GUI_PageEntry : MonoBehaviour
{

	public Text PageID;
	public Text VariableName;
	public InputField Variable;
	//public Text VariableType;
	public GUI_P_Bool boolP;
	public GUI_P_Input InputP;
	public GUI_P_Class ClassP;
	public GUI_P_Eume EumeP;
	public GUI_P_Collection CollectionP;

	public GUI_P_Bool PoolboolP;
	public GUI_P_Input PoolInputP;
	public GUI_P_Class PoolClassP;
	public GUI_P_Eume PoolEumeP;

	public GameObject CurrentlyShowing;

	public GameObject DynamicSizePanel;

	public bool NotPoolble;



	private VariableViewerNetworking.NetFriendlyPage _Page;
	public VariableViewerNetworking.NetFriendlyPage Page
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
	public void ValueSetUp()
	{

		Type t = Librarian.UEGetType(_Page.VariableType);
		bool pass = false;
		if (t != null)
		{

			//Logger.Log(t.ToString());
			if (t == typeof(bool))
			{
				GUI_P_Bool ValueEntry;
				if (PoolboolP == null)
				{
					ValueEntry = Instantiate(boolP) as GUI_P_Bool;
					PoolboolP = ValueEntry;
				}
				else {
					ValueEntry = PoolboolP;
					ValueEntry.gameObject.SetActive(true);
				}
	

				ValueEntry.transform.SetParent(DynamicSizePanel.transform);
				ValueEntry.transform.localScale = Vector3.one;
				ValueEntry.TToggle.isOn = bool.Parse(_Page.Variable);
				pass = true;
				CurrentlyShowing = ValueEntry.gameObject;

			}
			else if (t.IsGenericType)
			{

				GUI_P_Collection ValueEntry;

				ValueEntry = Instantiate(CollectionP) as GUI_P_Collection;
				ValueEntry.transform.SetParent(DynamicSizePanel.transform);
				ValueEntry.transform.localScale = Vector3.one;
				ValueEntry.TText.text = _Page.VariableName;
				ValueEntry.ID = _Page.ID;

				ValueEntry.Sentence = JsonConvert.DeserializeObject<VariableViewerNetworking.NetFriendlySentence>(_Page.Sentences);

				//ValueEntry.TDropdown.value = ValueEntry.TDropdown.options.IndexOf(_Page.Variable);//
				NotPoolble = true;
				CurrentlyShowing = ValueEntry.gameObject;
				pass = true;
			}
			else if (!t.IsValueType)
			{
				GUI_P_Class ValueEntry;
				if (PoolClassP == null)
				{
					ValueEntry = Instantiate(ClassP) as GUI_P_Class;
					PoolClassP = ValueEntry;
				}
				else {
					ValueEntry = PoolClassP;
					ValueEntry.gameObject.SetActive(true);
				}

		


				ValueEntry.transform.SetParent(DynamicSizePanel.transform);
				ValueEntry.transform.localScale = Vector3.one;
				ValueEntry.TText.text = _Page.Variable;
				ValueEntry.ID = _Page.ID;
				CurrentlyShowing = ValueEntry.gameObject;
				pass = true;

			}
			else if (t.IsEnum)
			{
				//IsEnum!!
				GUI_P_Eume ValueEntry;
				if (PoolEumeP == null)
				{
					ValueEntry = Instantiate(EumeP) as GUI_P_Eume;
					PoolEumeP = ValueEntry;
				}
				else {
					ValueEntry = PoolEumeP;
					ValueEntry.gameObject.SetActive(true);
				}
		

				ValueEntry.transform.SetParent(DynamicSizePanel.transform);
				ValueEntry.transform.localScale = Vector3.one;
				ValueEntry.TDropdown.ClearOptions();
				ValueEntry.TDropdown.captionText.text = _Page.Variable;
				var values = Enum.GetValues(t);
				List<string> llist = new List<string>();
				int Count = 0;
				int Selected = 0;
				foreach (var st in values)
				{
					if (st.ToString() == _Page.Variable)
					{
						Selected = Count;
					}
					llist.Add(st.ToString());
					Count++;
				}
				ValueEntry.TDropdown.AddOptions(llist);
				ValueEntry.TDropdown.value = Selected;
				//ValueEntry.TDropdown.value = ValueEntry.TDropdown.options.IndexOf(_Page.Variable);//
				CurrentlyShowing = ValueEntry.gameObject;
				pass = true;
			}

		}

		if (!pass)
		{
			GUI_P_Input ValueEntry;
			if (PoolInputP == null)
			{
				ValueEntry = Instantiate(InputP) as GUI_P_Input;
				PoolInputP = ValueEntry;
			}
			else {
				ValueEntry = PoolInputP;
				ValueEntry.gameObject.SetActive(true);
			}

			ValueEntry.transform.SetParent(DynamicSizePanel.transform);
			ValueEntry.transform.localScale = Vector3.one;
			ValueEntry.TInputField.text = _Page.Variable;
			CurrentlyShowing = ValueEntry.gameObject;

		}

	}
	public void Pool()
	{
		CurrentlyShowing.SetActive(false);
		if (NotPoolble) {
			Destroy(CurrentlyShowing);
			NotPoolble = false;
		}
	}
}
