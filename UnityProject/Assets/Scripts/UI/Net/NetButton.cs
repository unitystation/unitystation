using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
/// Simple button, has no special value
[RequireComponent(typeof( Button ))]
[Serializable]
public class NetButton : NetUIElement
{
	public UnityEvent ServerMethod;

	public override void ExecuteServer() {
		ServerMethod.Invoke();
	}
	
}