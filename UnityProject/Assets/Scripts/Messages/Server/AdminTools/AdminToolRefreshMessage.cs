using System.Collections;
using UnityEngine;
using Mirror;
using AdminTools;

public class AdminToolRefreshMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.AdminToolRefreshMessage;
	public string JsonData;
	public uint Recipient;

	public override IEnumerator Process()
	{
		yield return WaitFor(Recipient);
		var adminPageData = JsonUtility.FromJson<AdminPageRefreshData>(JsonData);

		var pages = GameObject.FindObjectsOfType<AdminPage>();
		foreach (var g in pages)
		{
			g.GetComponent<AdminPage>().OnPageRefresh(adminPageData);
		}

	}

	public static AdminToolRefreshMessage Send(GameObject recipient)
	{
		//Gather the data:
		var pageData = new AdminPageRefreshData();

		//Game Mode Information:
		pageData.availableGameModes = GameManager.Instance.GetAvailableGameModeNames();
		pageData.isSecret = GameManager.Instance.SecretGameMode;
		pageData.currentGameMode = GameManager.Instance.GetGameModeName(true);
		pageData.nextGameMode = GameManager.Instance.NextGameMode;

		var data = JsonUtility.ToJson(pageData);

		AdminToolRefreshMessage  msg =
			new AdminToolRefreshMessage  {Recipient = recipient.GetComponent<NetworkIdentity>().netId, JsonData = data};

		msg.SendTo(recipient);
		return msg;
	}
}
