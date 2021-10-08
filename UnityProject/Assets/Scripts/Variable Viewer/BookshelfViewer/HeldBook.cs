using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Messages.Client.VariableViewer;


namespace AdminTools.VariableViewer
{
	public class HeldBook : MonoBehaviour
	{
		public TMP_Text Name;
		public Image IMG;

		private VariableViewerNetworking.IDnName _IDANName;

		public VariableViewerNetworking.IDnName IDANName {
			get { return _IDANName; }
			set {
				Name.text = value.SN;
				_IDANName = value;
			}
		}

		public void OpenSpecifiedBook()
		{
			OpenBookIDNetMessage.Send(_IDANName.ID);
		}
	}
}
