using System;
using UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof( Button ))]
[Serializable]
public class TabHeaderButton : MonoBehaviour {
	public int Value { get; set; }

	private Button Button {
		get {
			if ( !button ) {
				button = GetComponent<Button>();
			}
			return button;
		}
	}
	private Button button;
	
	public Color selectedColor;
	public Color unselectColor;

	public void Select() {
		Button.image.color = selectedColor;
	}
	public void Unselect() {
		Button.image.color = unselectColor;
	}

	[HideInInspector]
	public IntEvent Method; //don't touch this

	private void OnDisable() {
		Method.RemoveAllListeners();
	}

	private void OnEnable() {
		Method.AddListener( i => GetComponentInParent<ControlTabs>()?.SelectTab( i ));
	}

	public void ClickExecute() {
		Method.Invoke(Value);
	}
}
/// <inheritdoc />
/// "If you wish to use a generic UnityEvent type you must override the class type."
[Serializable]
public class IntEvent : UnityEvent<int> {}