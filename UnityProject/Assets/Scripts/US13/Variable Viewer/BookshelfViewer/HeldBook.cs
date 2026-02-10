using TMPro;
using UnityEngine;
using UnityEngine.UI;
using US13.Messages.Client.VariableViewer;

namespace US13.Variable_Viewer.BookshelfViewer
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
