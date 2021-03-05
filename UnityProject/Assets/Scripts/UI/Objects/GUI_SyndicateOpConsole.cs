using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GUI_SyndicateOpConsole : NetTab
{
	private SyndicateOpConsole console;

	[SerializeField]
	private InputFieldFocus textComp;

	[SerializeField]
	private Text timer;

	public override void OnEnable()
	{
		base.OnEnable();
		StartCoroutine(WaitForProvider());
	}

	IEnumerator WaitForProvider()
	{
		while (Provider == null)
		{
			yield return WaitFor.EndOfFrame;
		}

		console = Provider.GetComponentInChildren<SyndicateOpConsole>();

		textComp.text = "A syndicate fringe group has declared their intent to utterly" +
		"destroy the station with a nuclear device and dares the crew to try and stop them";
	}

	private void UpdateTimer()
	{
		timer.text = $"{20 - console.Timer}:00";
	}


	public void ServerDeclareWar()
	{
		string DeclerationMessage = textComp.text;
		console.AnnounceWar(DeclerationMessage);
	}
}