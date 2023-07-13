using System;
using System.Collections;
using System.Collections.Generic;
using Systems.Character;
using UI.Character;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerBlueprint : MonoBehaviour, IServerSpawn
{
	public CharacterSheet CharacterSheet = new CharacterSheet();

	[FormerlySerializedAs("NonImportantMind")] [Tooltip("Should the mind be destroyed If the body is destroyed and a player is not possessing it")]
	public bool nonImportantMind = false;

	[NaughtyAttributes.Button()]
	public void Start()
	{
		if (this.GetComponentCustom<RuntimeSpawned>() == null) return;
		Spawn();

	}

	public void Spawn()
	{
		var Mind =  PlayerSpawn.NewSpawnCharacterV2(null, CharacterSheet, nonImportantMind);
		Mind.Body.GetComponent<UniversalObjectPhysics>().AppearAtWorldPositionServer(this.transform.position);
		_ = Despawn.ServerSingle(this.gameObject);
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		Spawn();
	}


}
