using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using NaughtyAttributes;

namespace UI.Core.NetUI
{
	/// Toggle for bool-based methods
	[RequireComponent(typeof(Toggle))]
	[Serializable]
	public class NetToggle : NetUIStringElement
	{
		public override string Value {
			get => Element.isOn ? "1" : "0";
			protected set {
				externalChange = true;
				Element.isOn = value.Equals("1");
				externalChange = false;
			}
		}

		[SerializeField]
		[InfoBox("If the toggle is part of a toggle group, and the toggles point to the same listeners below, " +
				"then they will be hit multiple times (each toggle, on / off). This is often not desirable. " +
				"A workaround is to only invoke the listener if the toggle is on, so the listener is only called once. " +
				"Check 'Enable Workaround' to enable this behaviour. ", EInfoBoxType.Normal)]
		// enough hours wasted on falling for the same mistake again and again... my darkest hours with that damned pipe dispenser
		private bool enableWorkaround = false;

		public BoolEvent ServerMethod;
		public BoolEventWithSubject ServerMethodWithSubject;

		public Toggle Element => element ??= GetComponent<Toggle>();
		private Toggle element;

		public override void ExecuteServer(PlayerInfo subject)
		{
			ServerMethod?.Invoke(Element.isOn);
			ServerMethodWithSubject?.Invoke(Element.isOn, subject);
		}

		public override void ExecuteClient()
		{
			if (enableWorkaround && Element.group != null && Element.isOn == false) return;
			base.ExecuteClient();
		}
	}
	/// <inheritdoc />
	/// "If you wish to use a generic UnityEvent type you must override the class type."
	[Serializable]
	public class BoolEvent : UnityEvent<bool> { }

	[Serializable]
	public class BoolEventWithSubject : UnityEvent<bool, PlayerInfo> { }
}
