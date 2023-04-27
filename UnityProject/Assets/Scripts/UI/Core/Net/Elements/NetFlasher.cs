using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Core.NetUI
{
	public class NetFlasher : NetUIStringElement
	{
		public float FlashSpeed = 0.2f;
		public float OffAlphaValue = 0.333f;
		public float OnAlphaValue = 1f;
		public override ElementMode InteractionMode => ElementMode.ServerWrite;

		public override string Value {
			get => State.ToString();
			protected set {
				if (State != bool.Parse(value))
				{
					State = bool.Parse(value);
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

		public void SetState(bool State)
		{
			Value = State.ToString();
		}

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

			var color = Element.color;
			color.a = LightState ? OffAlphaValue : OnAlphaValue;
			Element.color = color;
			LightState = !LightState;
		}

		private Graphic element;
		public Graphic Element => element ??= GetComponent<Graphic>();

		public override void ExecuteServer(PlayerInfo subject) { }

		/// <summary>
		/// Server-only method for updating element (i.e. changing label text) from server GUI code
		/// </summary>
		public override void MasterSetValue(string value)
		{
			if (Value != value)
			{
				Value = value;
				UpdatePeepers();
			}
		}
	}
}
