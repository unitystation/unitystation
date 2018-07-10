using System.Collections;
using System.Diagnostics.Eventing.Reader;
using UnityEngine;
using Util;

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
		if ( CustomNetworkManager.Instance._isServer ) {
//			Debug.Log( $"{name} Kinda init. Nuke code is {NukeInteract.NukeCode}" );
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

//ow
//	private static string tgt = "One day while Andy was toggling, " +
//	                            "Toggle got toggled. He could no longer help himself! " +
//	                            "He watched as Andy stroked his juicy kawaii toggle.";
//
//	private bool tgtMode;
//	public void ToggleGotToggled( bool toggled ) {
//		if ( toggled ) {
//			tgtMode = true;
//			StartCoroutine( ToggleStory(0) );
//		} else {
//			tgtMode = false;
//			InfoDisplay.SetValue = InitialInfoText;
//		}
//	}
//	private IEnumerator ToggleStory(int word) {
//		var strings = tgt.Split( ' ' );
//		InfoDisplay.SetValue = strings.Wrap(word);
//		yield return new WaitForSeconds( 0.15f );
//		if ( tgtMode ) {
//			StartCoroutine( ToggleStory(++word) );
//		}
//		
//	}

	private IEnumerator HideCode() {
		yield return new WaitForSeconds( 1 );
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
		yield return new WaitForSeconds( 1 );
		InfoDisplay.SetValue = "";
		yield return new WaitForSeconds( 1 );
		InfoDisplay.SetValue = "Incorrect code!";
		yield return new WaitForSeconds( 1 );
		InfoDisplay.SetValue = "";
		yield return new WaitForSeconds( 1 );
		
		cooldown = false;
		Clear();
		InfoDisplay.SetValue = InitialInfoText;
	}
}