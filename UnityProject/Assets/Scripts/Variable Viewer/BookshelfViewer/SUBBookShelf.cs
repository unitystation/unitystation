using System.Collections;
using System.Collections.Generic;
using DatabaseAPI;
using Messages.Client.VariableViewer;
using UnityEngine;
using UnityEngine.UI;

public class SUBBookShelf : MonoBehaviour
{
  	public Text Name;

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

	public void OpenBookshelf() {
		RequestBookshelfNetMessage.Send(_IDANName.ID, true, ServerData.UserID, PlayerList.Instance.AdminToken);
	}
}
