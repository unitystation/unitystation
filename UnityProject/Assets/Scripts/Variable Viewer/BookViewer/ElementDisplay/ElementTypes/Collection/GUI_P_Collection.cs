using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using Newtonsoft.Json;
using TMPro;

public class GUI_P_Collection : PageElement
{
	public override PageElementEnum PageElementType => PageElementEnum.Collection;

	public TMP_Text TText;
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
	public void ValueSetUp()
	{
		if (_Sentence != null && _Sentence.GetSentences() != null)
		{
			//Logger.LogError("yo1");
			//Logger.Log(JsonConvert.SerializeObject(_Sentence.GetSentences()));
			foreach (var bob in _Sentence.GetSentences())
			{
				//Logger.LogError("yo2");
				//Logger.Log("bob" + bob.SentenceID);
				SUB_ElementHandler ValueEntry = Instantiate(ElementHandler) as SUB_ElementHandler;
				ValueEntry.transform.SetParent(DynamicSizePanel.transform, false);
				ValueEntry.transform.localScale = Vector3.one;
				ValueEntry.Sentence = bob; //.GetSentences()
										   //Logger.Log(JsonConvert.SerializeObject(bob));
				ValueEntry.ValueSetUp();
			}
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
		//Logger.Log("A");
		if (Page != null)
		{
			//Logger.Log("B");
			Page.ProcessSentences();
			//Logger.Log(JsonConvert.SerializeObject(Page));
			if (Page.Sentences.Length > 0)
			{
				Data = Page.Sentences[0];
			}
		}
		else {
			if (Iskey)
			{
				Logger.LogError("WHAT?, GenericType Dictionary key?");
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
}
