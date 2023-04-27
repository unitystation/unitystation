using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Core.NetUI
{
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

		public override string Value {
			get => Element.text;
			protected set {
				externalChange = true;
				Element.text = value;
				externalChange = false;
			}
		}

		public InputField Element => element ??= GetComponent<InputField>();
		private InputField element;
	}
}
