using UnityEngine;
using US13.Messages.Client.VariableViewer;

namespace US13.Variable_Viewer.BookViewer.ElementDisplay
{
	public class SUB_ElementHandler : MonoBehaviour
	{
		public GameObject DynamicSizePanel;

		public VariableViewerNetworking.NetFriendlySentence Sentence;

		public GameObject UpButton;
		public GameObject DownButton;


		public void ValueSetUp()
		{
			VVUIElementHandler.ProcessElement(DynamicSizePanel, Sentence: Sentence, iskey: false);
			if (Sentence.KeyVariableType != "")
			{
				VVUIElementHandler.ProcessElement(DynamicSizePanel, Sentence: Sentence, iskey: true);
			}
		}

		public void UpdateButtons()
		{

			var index = transform.GetSiblingIndex();
			if (index == 0)
			{
				UpButton.gameObject.SetActive(false);
			}
			else
			{
				UpButton.gameObject.SetActive(true);
			}


			if (index == (transform.parent.childCount - 1))
			{
				DownButton.SetActive(false);
			}
			else
			{
				DownButton.SetActive(true);
			}
		}

		public void Remove()
		{
			RequestChangeVariableNetMessage.Send(Sentence.OnPageID, Sentence.SentenceID.ToString(),
				UISendToClientToggle.toggle, 0, false,VariableViewer.ListModification.Remove);

			var PageElements = this.GetComponentsInChildren<PageElement>();

			foreach (var PageElement in PageElements)
			{
				VVUIElementHandler.CurrentlyOpen.Remove(PageElement);
			}


			Destroy(this.gameObject);

			int children = transform.parent.childCount;
			for (int i = 0; i < children; ++i)
				transform.parent.GetChild(i).GetComponent<SUB_ElementHandler>().UpdateButtons();
		}

		public void Up()
		{
			RequestChangeVariableNetMessage.Send(Sentence.OnPageID, Sentence.SentenceID.ToString(),
				UISendToClientToggle.toggle, uint.MaxValue, false,VariableViewer.ListModification.Up);

			var currentIndex = transform.GetSiblingIndex();
			var NewIndex = currentIndex - 1;
			var Replacing = transform.parent.GetChild(NewIndex).GetComponent<SUB_ElementHandler>();


			transform.SetSiblingIndex(NewIndex);
			UpdateButtons();
			Replacing.UpdateButtons();
		}
		public void Down()
		{
			RequestChangeVariableNetMessage.Send(Sentence.OnPageID, Sentence.SentenceID.ToString(),
				UISendToClientToggle.toggle, uint.MaxValue, false ,  VariableViewer.ListModification.Down);

			var currentIndex = transform.GetSiblingIndex();
			var NewIndex = currentIndex + 1;

			var Replacing = transform.parent.GetChild(NewIndex).GetComponent<SUB_ElementHandler>();

			transform.SetSiblingIndex(NewIndex);
			UpdateButtons();
			Replacing.UpdateButtons();
		}


	}
}
