using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI.Core.NetUI;
using UnityEngine;

public class NetTMPInputField : NetUIStringElement
{
	public override string Value {
		get => Element.text;
		protected set {
			externalChange = true; //TODO looking to WHY???
			Element.text = value;
			externalChange = false;
		}
	}

	public TMP_InputField Element => element ??= GetComponent<TMP_InputField>();
	private TMP_InputField element;

	public void Awake()
	{
		Element.onEndEdit.AddListener(ClientFinishEditing);
	}


	public void ClientFinishEditing(string newValue)
	{
		ExecuteClient();
	}
}
