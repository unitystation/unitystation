using System.Collections;
using UnityEngine;

public class GUI_SyndicateOpConsole : NetTab
{
	private SyndicateOpConsole console;

	[SerializeField]
	private InputFieldFocus textComp;

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


	public void ServerDeclareWar()
	{
		string DeclerationMessage = textComp.text;
		console.AnnounceWar(DeclerationMessage);
	}
}