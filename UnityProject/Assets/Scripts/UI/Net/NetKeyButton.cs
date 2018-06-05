using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof( Button ))]
[Serializable]
public class NetKeyButton : NetUIElement
{
	public override string Value => name.Length == 1 ? name : "-1";

	public CharEvent ServerMethod;

	public override void ExecuteServer() {
		ServerMethod.Invoke(name.ToCharArray()[0]);
	}
	
}
/// <inheritdoc />
/// "If you wish to use a generic UnityEvent type you must override the class type."
[Serializable]
public class CharEvent : UnityEvent<char>
{
}