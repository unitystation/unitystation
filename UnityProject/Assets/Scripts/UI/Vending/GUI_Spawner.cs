using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_Spawner : NetTab 
{
	private ItemList entryList;
	private ItemList EntryList {
		get {
			if ( !entryList ) {
				entryList = this["EntryList"] as ItemList;
			}
			return entryList;
		} 
	}
//	private ItemList EntryList => this["EntryList"] as ItemList;
	
	private void Start() {
		//Not doing this for clients
		if ( IsServer ) {
			// Add items from InitialContents list
			List<GameObject> initList = Provider.GetComponent<SpawnerInteract>().InitialContents;
			for ( var i = 0; i < initList.Count; i++ ) {
				var item = initList[i];
				EntryList.AddItem( item );
			}
		}
	}

	public void AddItem( string prefabName ) {
		EntryList.AddItem( prefabName );
	}

	public void RemoveItem( string prefabName ) {
		EntryList.RemoveItem( prefabName );
	}

	public void SpawnItemByIndex( string index ) {
		ItemEntry item = GetItemFromIndex( index );
		var prefab = item?.Prefab;
//		Logger.Log( $"Spawning item '{prefab?.name}'!" );
		
		Vector3 originPos = Provider.WorldPos();
		Vector3 nearestPlayerPos = GetNearestPlayerPos(originPos);

		if ( nearestPlayerPos == TransformState.HiddenPos ) {
			return;
		}
		
		var spawnedItem = ItemFactory.SpawnItem( prefab, originPos );
		spawnedItem.GetComponent<CustomNetTransform>()?.Throw( new ThrowInfo {
			ThrownBy = Provider,
			Aim = BodyPartType.Chest,
			OriginPos = originPos,
			TargetPos = nearestPlayerPos, //haha
			SpinMode = SpinMode.CounterClockwise
		} );
	}

	///Tries to get nearest player's position within range, and returns HiddenPos if it fails
	///could be moved to some util class, gonna be useful
	private Vector3 GetNearestPlayerPos( Vector3 originPos, int maxRange = 10 ) 
	{
		float smallestDistance = float.MaxValue;
		Vector3 nearestPosSoFar = TransformState.HiddenPos;

		for ( var i = 0; i < PlayerList.Instance.InGamePlayers.Count; i++ ) 
		{
			ConnectedPlayer player = PlayerList.Instance.InGamePlayers[i];
			float curDistance = Vector3.Distance( originPos, player.Script.WorldPos );
		
			if ( curDistance < smallestDistance ) {
				smallestDistance = curDistance;
				nearestPosSoFar = player.Script.WorldPos;
			}
		}

		if ( smallestDistance <= maxRange ) {
			return nearestPosSoFar;
		}
		return TransformState.HiddenPos;
	}

	private bool firingMode;

	public void ToggleFire() {
		firingMode = !firingMode;
		if ( firingMode ) {
			StartCoroutine( KeepFiring(0) );
		} 
	}
	
	private IEnumerator KeepFiring(int shot) {
		var strings = EntryList.Value.Split( new[]{','}, StringSplitOptions.RemoveEmptyEntries );
		if ( strings.Length > 0 ) {
			//See, this is pretty cool
			string s = strings.Wrap( shot );
			//fire
			SpawnItemByIndex( s );
		}
		yield return new WaitForSeconds( 1.5f );
		if ( firingMode ) {
			StartCoroutine( KeepFiring(++shot) );
		}
	}

	public void RemoveItemByIndex( string index ) {
		RemoveItem( GetItemFromIndex(index)?.Prefab.name );
	}

	private ItemEntry GetItemFromIndex(string index) {
		return EntryList.EntryIndex[index] as ItemEntry;
	}
}
