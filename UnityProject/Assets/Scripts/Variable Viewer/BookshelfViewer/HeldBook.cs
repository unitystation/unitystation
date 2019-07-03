using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeldBook : MonoBehaviour
{
	public Text Name;
	public Image IMG;


	private VariableViewerNetworking.IDnName _IDANName;
	public VariableViewerNetworking.IDnName IDANName
	{
		get { return _IDANName; }
		set
		{
			Name.text = value.SN;
			_IDANName = value;
		}
	}

	public void OpenSpecifiedBook() {
		OpenBookIDNetMessage.Send(_IDANName.ID);
	}
}
