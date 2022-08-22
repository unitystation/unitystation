using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Messages.Client.VariableViewer;


namespace AdminTools.VariableViewer
{
	public class SUBBookShelf : MonoBehaviour
	{
		public Text Name;

		private VariableViewerNetworking.IDnName _IDANName;
		public VariableViewerNetworking.IDnName IDANName {
			get { return _IDANName; }
			set {
				Name.text = value.SN;
				_IDANName = value;
			}
		}

		public void OpenBookshelf()
		{
			RequestBookshelfNetMessage.Send(_IDANName.ID, true);
		}
	}
}
