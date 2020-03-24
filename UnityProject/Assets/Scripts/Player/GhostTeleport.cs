using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Teleporting Ghosts to other players 
/// </summary>
public class GhostTeleport : MonoBehaviour
{
	//Dictionary of UniqueKey(int), data 
	public IDictionary<int, dynamic> MobList = new Dictionary<int, dynamic>();

	public int Count = 0;
	private string NameOfObject;
	private string Status;
	private Vector3 Position;
	private PlayerManager playerManager;
	private PlayerSync playerSync;

	private void Start()
	{
		playerManager = PlayerManager.Instance;
		playerSync = GetComponent<PlayerSync>();
	}

	//Dictionary of Key(int), object/mob name, status (alive/dead/ghost/none), postion(vector3)
	private void AddToDict()
	{
		dynamic d1 = new System.Dynamic.ExpandoObject();
		MobList[Count] = d1;
		MobList[Count].Data = new { s1 = NameOfObject, s2 = Status, s3 = Position};
		Count += 1;
	}

	public void FindData()
	{
		MobList.Clear();
		Count = 0;
		var PlayerBodies = FindObjectsOfType(typeof(PlayerScript));

		Logger.Log("Players:" + PlayerBodies);

		if (PlayerBodies == null | PlayerBodies.Count() == 0)
		{
			Logger.Log("PlayerBodies was null or 0");
		}
		else
		{
			foreach (PlayerScript player in PlayerBodies)
			{
				//Gets Name of Player
				NameOfObject = player.name;

				//Gets Status of Player
				if (player.IsGhost)
				{
					Status = "(Ghost)";
				}
				else if(!player.IsGhost & player.playerHealth.IsDead)
				{
					Status = "(Dead)";
				}
				else if (!player.IsGhost)
				{
					Status = "(Alive)";
				}
				else
				{
					Status = "(Cant tell if Dead/Alive or Ghost)";
				}

				//Gets Position of Player
				var tile = player.gameObject.GetComponent<RegisterTile>();
				Position = tile.WorldPositionServer;


				Logger.Log("name:" + NameOfObject);
				Logger.Log("Status" + Status);
				Logger.Log("Position" + Position);
				AddToDict();
			}
		}		
	}

	public void DataForTeleport(int index)
	{
		var s3 = MobList[index].Data.s3;
		GameObject localPlayer = PlayerManager.LocalPlayer;
		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdGhostPerformTeleport(index, s3, localPlayer);
	}
}
