using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class GUI_P_Collection : MonoBehaviour
{
	public Text TText;
	public Text ButtonText;
	public GameObject Page;
	public GameObject DynamicSizePanel;
	public bool IsOpen;
	public ulong ID;

	public SUB_ElementHandler ElementHandler;
	private VariableViewerNetworking.NetFriendlySentence _Sentence;
	public VariableViewerNetworking.NetFriendlySentence Sentence
	{
		get { return _Sentence; }
		set
		{
			_Sentence = value;
			ValueSetUp();
		}
	}
	public void ValueSetUp() {
		if (_Sentence != null && _Sentence.Sentences != null)
		{
			foreach (var bob in _Sentence.Sentences)
			{
				SUB_ElementHandler ValueEntry = Instantiate(ElementHandler) as SUB_ElementHandler;
				ValueEntry.transform.SetParent(DynamicSizePanel.transform, false);
				ValueEntry.transform.localScale = Vector3.one;
				ValueEntry.Sentence = bob;
				ValueEntry.ValueSetUp();
			}
		}
	}

	public void TogglePage()
	{
		if (_Sentence != null)
		{
			IsOpen = !IsOpen;
			if (IsOpen)
			{
				ButtonText.text = "X";
				Page.SetActive(true);

			}
			else {
				ButtonText.text = "\\/";
				Page.SetActive(false);
			}
		}
	}
}
