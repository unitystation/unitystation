using System;
using System.Collections;
using System.Collections.Generic;
using Systems.Character;
using UI.Character;
using UnityEngine;

public class PlayerBlueprint : MonoBehaviour
{
	public CharacterSheet CharacterSheet = new CharacterSheet();

	[Tooltip("Should the mind be destroyed If the body is destroyed and a player is not possessing it")]
	public bool NonImportantMind = false;

	[NaughtyAttributes.Button()]
	public void Start()
	{
		var Mind =  PlayerSpawn.NewSpawnCharacterV2(null, CharacterSheet, NonImportantMind);
		Mind.Body.GetComponent<UniversalObjectPhysics>().AppearAtWorldPositionServer(this.transform.position);
	}


}
