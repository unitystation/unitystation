using System.Collections;
using System.Collections.Generic;
using DatabaseAPI;
using Messages.Client.VariableViewer;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HeldBook : MonoBehaviour
{
	public TMP_Text Name;
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

	public void OpenSpecifiedBook()
	{
		OpenBookIDNetMessage.Send(_IDANName.ID, ServerData.UserID, PlayerList.Instance.AdminToken);
	}
}