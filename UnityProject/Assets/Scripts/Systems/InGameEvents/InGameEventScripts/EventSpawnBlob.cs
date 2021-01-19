using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Antagonists;
using Blob;
using UnityEngine;
using InGameEvents;
using ScriptableObjects;

public class EventSpawnBlob : EventScriptBase
{
	[SerializeField]
	private Antagonist blobAntag = default;

	public override void OnEventStart()
	{
		if (!FakeEvent)
		{
			InfectRandomPerson();
		}

		base.OnEventStart();
	}

	private void InfectRandomPerson()
	{
		var player = PlayerList.Instance.InGamePlayers
			.Where(p => !p.Script.IsDeadOrGhost
			            && PlayerList.HasAntagEnabled(p, blobAntag)
			            && PlayerList.Instance.CheckJobBanState(p.UserId, JobType.BLOB)
			            && p.GameObject.GetComponent<BlobStarter>() == null
			            && p.GameObject.GetComponent<BlobPlayer>() == null).PickRandom();

		if(player == null) return;

		player.GameObject.AddComponent<BlobStarter>();

		var playerScript = player.GameObject.GetComponent<PlayerScript>();

		var antag = playerScript.mind.GetAntag();

		//Set up objectives
		if (antag == null || antag.Antagonist.AntagJobType != JobType.BLOB)
		{
			AntagManager.Instance.ServerFinishAntag(SOAdminJobsList.Instance.Antags.First(a => a.AntagJobType == JobType.BLOB), player);
		}
	}
}
