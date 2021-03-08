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
	}

	private void UpdateTimer(ConnectedPlayer player)
	{
		timer.text = $"{20 - console.Timer}:00";
	}


	public void ServerDeclareWar(string DeclerationMessage)
	{
		console.AnnounceWar(DeclerationMessage);
	}
}