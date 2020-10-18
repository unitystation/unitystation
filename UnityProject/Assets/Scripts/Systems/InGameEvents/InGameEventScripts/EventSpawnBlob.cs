using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blob;
using UnityEngine;
using InGameEvents;

public class EventSpawnBlob : EventScriptBase
{
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
		var player = PlayerList.Instance.InGamePlayers.Where(p => !p.Script.IsDeadOrGhost).PickRandom();

		player.GameObject.AddComponent<BlobStarter>();
	}
}
