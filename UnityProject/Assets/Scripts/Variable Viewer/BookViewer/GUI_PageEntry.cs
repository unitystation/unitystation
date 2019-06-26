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
		VVUIElementHandler.ProcessElement(DynamicSizePanel, _Page);
	}


}
