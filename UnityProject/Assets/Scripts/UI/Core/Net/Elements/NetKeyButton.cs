using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Core.NetUI
{
	/// Used to call Char methods.
	/// Uses first char of its own name as value.
	/// Useful for virtual keypads.
	[RequireComponent(typeof(Button))]
	[Serializable]
	public class NetKeyButton : NetUIStringElement
	{
		public override string Value => name.ToCharArray()[0].ToString();

		public CharEvent ServerMethod;

		public override void ExecuteServer(PlayerInfo subject)
		{
			ServerMethod.Invoke(name.ToCharArray()[0]);
		}
	}

	/// <inheritdoc />
	/// "If you wish to use a generic UnityEvent type you must override the class type."
	[Serializable]
	public class CharEvent : UnityEvent<char> { }
}
