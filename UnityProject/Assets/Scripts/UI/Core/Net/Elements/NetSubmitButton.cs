using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Core.NetUI
{
	/// Submit button for client text input field.
	/// Sends client's InputField value to server method
	[RequireComponent(typeof(Button))]
	[Serializable]
	public class NetSubmitButton : NetUIStringElement
	{
		public override ElementMode InteractionMode => ElementMode.ClientWrite;

		public override string Value {
			get => SourceInputField?.text ?? "-1";
			protected set {
				externalChange = true;
				SourceInputField.text = value;
				externalChange = false;
			}
		}
		public StringEvent ServerMethod;
		public InputField SourceInputField;

		public override void ExecuteServer(PlayerInfo subject)
		{
			ServerMethod.Invoke(Value);
		}
	}
	/// <inheritdoc />
	/// "If you wish to use a generic UnityEvent type you must override the class type."
	[Serializable]
	public class StringEvent : UnityEvent<string> { }
}
