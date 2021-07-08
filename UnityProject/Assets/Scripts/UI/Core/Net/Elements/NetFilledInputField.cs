using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lets server set initial text for input field.
/// Don't use it to enforce some text:
/// It's intended for suggestions or existing values
/// </summary>
[RequireComponent(typeof(InputField))]
[Serializable]
public class NetFilledInputField : NetUIStringElement
{
	public override ElementMode InteractionMode => ElementMode.ServerWrite;

	public override string Value
	{
		get { return Element.text; }
		set
		{
			externalChange = true;
			Element.text = value;
			externalChange = false;
		}
	}

	private InputField element;

	public InputField Element
	{
		get
		{
			if ( !element )
			{
				element = GetComponent<InputField>();
			}

			return element;
		}
	}

	public override void ExecuteServer(ConnectedPlayer subject)
	{
	}
}