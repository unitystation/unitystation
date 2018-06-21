using System.Collections;
using UnityEngine;
using Util;

public class GUI_Spawner : NetTab 
{
	private ItemList EntryList => this["EntryList"] as ItemList;

	public void AddItem( string prefabName ) {
		EntryList?.AddItem( prefabName );
		curIndex = EntryList?.Value.Split( ',' )[0];
	}

	public void RemoveItem( string prefabName ) {
		EntryList?.RemoveItem( prefabName );
	}

	public void SpawnItemByIndex( string index ) {
		ItemEntry item = GetItemFromIndex( index );
		var prefab = item?.Prefab;
//		Debug.Log( $"Spawning item '{prefab?.name}'!" );
		
		Vector3 originPos = Provider.WorldPos();
		Vector3 nearestPlayerPos = GetNearestPlayerPos(originPos);

		if ( nearestPlayerPos == TransformState.HiddenPos ) {
			return;
		}
		
		var spawnedItem = ItemFactory.SpawnItem( prefab, originPos );
		spawnedItem.GetComponent<CustomNetTransform>()?.Throw( new ThrowInfo {
			ThrownBy = Provider,
			Aim = BodyPartType.CHEST,
			OriginPos = originPos,
			TargetPos = nearestPlayerPos, //haha
			//originPos + (Vector3)new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f)),
			SpinMode = SpinMode.CounterClockwise
		} );
	}

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
	private string curIndex;

	public void ToggleFire() {
		firingMode = !firingMode;
		if ( firingMode ) {
			StartCoroutine( KeepFiring() );
		} 
	}
	private IEnumerator KeepFiring() {
		//fire
		SpawnItemByIndex( curIndex );
		yield return new WaitForSeconds( 1.5f );
		if ( firingMode ) {
			StartCoroutine( KeepFiring() );
		}
	}

	public void RemoveItemByIndex( string index ) {
		RemoveItem( GetItemFromIndex(index)?.Prefab.name );
	}

//	private string NextIndex( string curIndex ) {
//		
//	}

	private ItemEntry GetItemFromIndex(string index) {
		return EntryList.Entries[index] as ItemEntry;
//		var itemListEntries = EntryList?.Entries;
//		if ( itemListEntries != null && itemListEntries.ContainsKey( index ) ) {
//			return itemListEntries[index] as ItemEntry;
//		}
//		return null;
	}
}
