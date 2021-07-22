using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AGhost : MonoBehaviour
{
	public void OnClick()
	{
		if (PlayerManager.LocalPlayerScript == null) return;
		var adminId = DatabaseAPI.ServerData.UserID;
		var adminToken = PlayerList.Instance.AdminToken;
		PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdAGhost(adminId, adminToken);
	}
}
