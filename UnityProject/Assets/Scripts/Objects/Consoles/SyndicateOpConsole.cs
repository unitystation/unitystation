using UnityEngine;
using Managers;
using Strings;

public class SyndicateOpConsole : MonoBehaviour
{
	public void AnnounceWar(string DeclerationMessage)
	{
		GameManager.Instance.CentComm.ChangeAlertLevel(CentComm.AlertLevel.Red, true);
		CentComm.MakeAnnouncement(ChatTemplates.PriorityAnnouncement, 
		$"Attention all crew! An open message from the syndicate has been picked up on local radiowaves! Message Reads:\n" +
		$"{DeclerationMessage}" ,CentComm.UpdateSound.Alert);
	}
}
