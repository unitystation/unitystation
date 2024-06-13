using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using Logs;
using Messages.Client.VariableViewer;
using Newtonsoft.Json;
using TMPro;

public class GUI_P_Collection : PageElement
{
	public override PageElementEnum PageElementType => PageElementEnum.Collection;

	public TMP_Text TText;
	public TMP_Text ButtonText;
	public GameObject Page;
	public GameObject DynamicSizePanel;
	public bool IsOpen;
	public ulong ID;

	public SUB_ElementHandler ElementHandler;

	public List<SUB_ElementHandler> LoadedElements = new List<SUB_ElementHandler>();


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
	public void ValueSetUp()
	{
		if (_Sentence != null && _Sentence.GetSentences() != null)
		{
			//Loggy.LogError("yo1");
			//Loggy.Log(JsonConvert.SerializeObject(_Sentence.GetSentences()));
			foreach (var bob in _Sentence.GetSentences())
			{
				//Loggy.LogError("yo2");
				//Loggy.Log("bob" + bob.SentenceID);
				SUB_ElementHandler ValueEntry = Instantiate(ElementHandler) as SUB_ElementHandler;
				ValueEntry.transform.SetParent(DynamicSizePanel.transform, false);
				ValueEntry.transform.localScale = Vector3.one;
				ValueEntry.Sentence = bob; //.GetSentences()
										   //Loggy.Log(JsonConvert.SerializeObject(bob));
				ValueEntry.ValueSetUp();
				LoadedElements.Add(ValueEntry);
			}
		}

		foreach (var Element in LoadedElements)
		{
			Element.UpdateButtons();
		}

	}

	public override bool IsThisType(Type TType)
	{
		if (TType.IsGenericType)
		{
			return (true);
		}
		else {
			return (false);
		}
	}

	public override void SetUpValues(Type ValueType, VariableViewerNetworking.NetFriendlyPage Page = null, VariableViewerNetworking.NetFriendlySentence Sentence = null, bool Iskey = false)
	{
		VariableViewerNetworking.NetFriendlySentence Data = new VariableViewerNetworking.NetFriendlySentence();
		//Loggy.Log("A");

		TText.text = ValueType.ToString();
		if (Page != null)
		{
			//Loggy.Log("B");
			Page.ProcessSentences();
			//Loggy.Log(JsonConvert.SerializeObject(Page));
			if (Page.Sentences.Length > 0)
			{
				Data = Page.Sentences[0];
			}
			else
			{
				Data = new VariableViewerNetworking.NetFriendlySentence();
			}
			Data.OnPageID = Page.ID;
		}
		else {
			if (Iskey)
			{
				Loggy.LogError("WHAT?, GenericType Dictionary key?", Category.VariableViewer);
			}
			else {
				Data = Sentence;
			}
		}

		this.Sentence = Data;
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

	public void AddElement()
	{
		RequestChangeVariableNetMessage.Send(Sentence.OnPageID, "",
			UISendToClientToggle.toggle,uint.MaxValue,VariableViewer.ListModification.Add);
		StartCoroutine(Refresh());
	}
	private IEnumerator Refresh()
	{
		yield return null;
		yield return null;
		UIManager.Instance.VariableViewer.Refresh();
	}


}
