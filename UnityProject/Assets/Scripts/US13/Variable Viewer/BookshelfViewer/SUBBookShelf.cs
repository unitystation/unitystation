using UnityEngine;
using UnityEngine.UI;
using US13.Core.Input_System;
using US13.Messages.Client.VariableViewer;
using US13.Variable_Viewer.BookViewer.ElementDisplay.ElementTypes.Component;

namespace US13.Variable_Viewer.BookshelfViewer
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
			if (GUI_P_Component.VVObjectComponentSelectionActive == false)
			{
				RequestBookshelfNetMessage.Send(_IDANName.ID, true, KeyboardInputManager.IsAltActionKeyPressed());
			}
			else
			{
				GUI_P_Component.ActiveComponent.SetBook(_IDANName.ID);
				GUI_P_Component.ActiveComponent.Close();
			}
		}
	}
}
