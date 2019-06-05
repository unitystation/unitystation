using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;

public class GUI_PageEntry : MonoBehaviour
{

	public Text PageID;
	public Text VariableName;
	public InputField Variable;
	//public Text VariableType;


	private BookNetMessage.NetFriendlyPage _Page;
	public BookNetMessage.NetFriendlyPage Page
	{
		get { return _Page; }
		set
		{
			PageID.text = "ID > " + value.ID;
			VariableName.text = value.VariableName;
			Variable.text = value.Variable;
			//VariableType.text = " VariableType > " + value.VariableType;
			_Page = value;
		}
	}

}
