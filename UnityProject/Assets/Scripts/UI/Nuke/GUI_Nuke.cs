﻿using System.Collections;
using UnityEngine;

public class GUI_Nuke : NetTab
{
	private NukeInteract nukeInteract;
	private NukeInteract NukeInteract {
		get {
			if ( !nukeInteract ) {
				nukeInteract = Provider.GetComponent<NukeInteract>();
			}

			return nukeInteract;
		}
	}
	private bool cooldown;

	//define elements you want to visually update here

	//Example: with caching (uglier, but cheaper)
	private NetUIElement infoDisplay;
	private NetUIElement InfoDisplay {
		get {
			if ( !infoDisplay ) {
				infoDisplay = this["InfoDisplay"];
			}
			return infoDisplay;
		}
	}
	//Example: without caching (prettier, more expensive)
	private NetUIElement CodeDisplay => this["CodeDisplay"];

	private string InitialInfoText;

	private void Start() {
		//Not doing this for clients
		if ( IsServer ) {
//			Logger.Log( $"{name} Kinda init. Nuke code is {NukeInteract.NukeCode}" );
			InitialInfoText = $"Enter {NukeInteract.NukeCode.ToString().Length}-digit code:";
			InfoDisplay.SetValue = InitialInfoText;
		}
	}

	public void EnterDigit(char digit) {
		if ( cooldown ) {
			return;
		}
		if ( NukeInteract.AppendKey( digit ) ) {
			int length = NukeInteract.CurrentCode.Length;
			//replace older digits with asterisks
			string newDigit = NukeInteract.CurrentCode.Substring( length <= 0 ? 0 : length - 1 );
			CodeDisplay.SetValue = newDigit.PadLeft( length, '*' );
			StartCoroutine( HideCode() );
		}
	}

	private IEnumerator HideCode() {
		yield return WaitFor.Seconds( 1 );
		CodeDisplay.SetValue = "".PadLeft( CodeDisplay.Value.Length, '*' );
	}

	public void Clear() {
		if ( cooldown ) {
			return;
		}
		NukeInteract.Clear();
		CodeDisplay.SetValue = "";
	}
	public void TryArm() {
		if ( cooldown ) {
			return;
		}

		if (NukeInteract.Validate()) {
			InfoDisplay.SetValue = "PREPARE TO DIE";
		} else {
			StartCoroutine( ErrorCooldown() );
		}

	}

	public IEnumerator ErrorCooldown() {
		cooldown = true;
		InfoDisplay.SetValue = "Incorrect code!";
		yield return WaitFor.Seconds( 0.5F );
		InfoDisplay.SetValue = "";
		yield return WaitFor.Seconds( 0.5F );
		InfoDisplay.SetValue = "Incorrect code!";
		yield return WaitFor.Seconds( 0.5F );
		InfoDisplay.SetValue = "";
		yield return WaitFor.Seconds( 0.5F );
		InfoDisplay.SetValue = "Incorrect code!";
		yield return WaitFor.Seconds( 0.5F );
		InfoDisplay.SetValue = "";
		yield return WaitFor.Seconds( 0.5F );
		cooldown = false;
		Clear();
		InfoDisplay.SetValue = InitialInfoText;
	}
}