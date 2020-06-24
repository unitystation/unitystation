using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NetFlasher : NetUIStringElement
{
	public float FlashSpeed = 0.2f;
	public float OffAlphaValue = 0.333f;
	public float OnAlphaValue = 1f;
	public override ElementMode InteractionMode => ElementMode.ServerWrite;

	public void SetState(bool State)
	{
		Value = State.ToString();
	}
	public override string Value
	{
		get
		{
			return (State).ToString();
		}
		set
		{
			if (State != Boolean.Parse(value))
			{
				State = Boolean.Parse(value);
				CheckState();
			}
		}
	}

	public bool State = false;

	public bool LightState = false;

	/*
	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, ToggleBlink);
		LightState = true;
		ToggleBlink();
	}
	*/

	public void CheckState()
	{
		if (State)
		{
			UpdateManager.Add(ToggleBlink, FlashSpeed);
		}
		else
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, ToggleBlink);
			LightState = true;
			ToggleBlink();
		}
	}

	public void ToggleBlink()
	{
		if (this == null)
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, ToggleBlink);
			return;
		};

		if (LightState)
		{
			var colo = Element.color;
			colo.a = OffAlphaValue;
			Element.color = colo;
			LightState = !LightState;
		}
		else
		{
			var colo = Element.color;
			colo.a = OnAlphaValue;
			Element.color = colo;
			LightState = !LightState;
		}
	}

	private Graphic element;
	public Graphic Element {
		get {
			if ( !element ) {
				element = GetComponent<Graphic>();
			}
			return element;
		}
	}

	public override void ExecuteServer(ConnectedPlayer subject) {	}

	/// <summary>
	/// Server-only method for updating element (i.e. changing label text) from server GUI code
	/// </summary>
	public override void SetValueServer(string value)
	{
		if (Value != value)
		{
			Value = value;
			UpdatePeepers();
		}
	}

}
