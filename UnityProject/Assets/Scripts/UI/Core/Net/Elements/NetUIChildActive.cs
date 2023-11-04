using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UI.Core.NetUI;
using UnityEngine;

public class NetUIChildActive : NetUIStringElement
{

	public GameObject ToToggleChild;

	public override string Value {
		get => ToToggleChild.activeSelf ? "1" : "0";
		protected set {
			externalChange = true;
			ToToggleChild.SetActive(value.Equals("1"));
			externalChange = false;
			NetworkTabManager.Instance.Rescan(containedInTab.NetTabDescriptor);
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

	public override void ExecuteServer(PlayerInfo subject)
	{

	}

	public override void ExecuteClient()
	{
		if (enableWorkaround && ToToggleChild.activeSelf == false) return;
		base.ExecuteClient();
	}

	public void MasterNetSetActive(bool activity)
	{
		MasterSetValue(activity ? "1" : "0");

	}
}
