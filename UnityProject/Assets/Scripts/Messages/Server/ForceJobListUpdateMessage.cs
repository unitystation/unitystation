using System.Collections;
using PlayGroup;
using UI;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Message that tells client to update thier job lists
/// </summary>
public class ForceJobListUpdateMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.ForceJobListUpdateMessage;

	public override IEnumerator Process()
	{

		GUI_PlayerJobs playerJobs = UIManager.Instance.displayControl.jobSelectWindow.GetComponent<GUI_PlayerJobs>();
		playerJobs.isUpToDate = false;

		if (!GameData.IsHeadlessServer && PlayerManager.LocalPlayerScript.JobType == JobType.NULL)
		{
			//Reset required if player played in previous round
			playerJobs.hasPickedAJob = false;
			Debug.Log("has picked job reset");
		}

		yield return null;
	}

	public static ForceJobListUpdateMessage Send()
	{
		ForceJobListUpdateMessage msg = new ForceJobListUpdateMessage();
		msg.SendToAll();
		return msg;
	}

	public override string ToString()
	{
		return $"[ForceJobListUpdateMessage Type={MessageType}]";
	}
}