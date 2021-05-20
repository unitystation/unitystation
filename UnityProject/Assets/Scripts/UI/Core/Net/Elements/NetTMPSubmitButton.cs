using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace UI.Core.Net.Elements
{
	/// <summary>
	/// Submit button for client TextMeshPro input field.
	/// Sends client's TMP_InputField value to server method.
	/// </summary>
	[RequireComponent(typeof(Button))]
	[Serializable]
	public class NetTMPSubmitButton : NetUIStringElement
	{
		public override ElementMode InteractionMode => ElementMode.ClientWrite;

		public override string Value
		{
			get { return SourceInputField?.text ?? "-1"; }
			set
			{
				externalChange = true;
				SourceInputField.text = value;
				externalChange = false;
			}
		}
		public StringEvent ServerMethod;
		public TMP_InputField SourceInputField;

		public override void ExecuteServer(ConnectedPlayer subject)
		{
			ServerMethod.Invoke(Value);
		}
	}
}