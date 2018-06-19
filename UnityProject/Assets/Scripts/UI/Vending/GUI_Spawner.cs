using UnityEngine;
using Util;

public class GUI_Spawner : NetUITab 
{
	private ItemList EntryList => Info["EntryList"] as ItemList;

	public void AddItem( string prefabName ) {
		EntryList?.AddItem( prefabName );
	}

	public void RemoveItem( string prefabName ) {
		EntryList?.RemoveItem( prefabName );
	}

	public void SpawnItemByIndex( string index ) {
		var item = GetItemFromIndex( index );
		var prefab = item?.Prefab;
		Debug.Log( $"Spawning item '{prefab?.name}'!" );
		Vector3 originPos = Provider.WorldPos();
		var spawnedItem = ItemFactory.SpawnItem( prefab, originPos );
		spawnedItem.GetComponent<CustomNetTransform>()?.Throw( new ThrowInfo {
			ThrownBy = Provider,
			Aim = BodyPartType.CHEST,
			OriginPos = originPos,
			TargetPos = originPos + (Vector3)new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f)),
			SpinMode = SpinMode.CounterClockwise
		} );
	}

	public void RemoveItemByIndex( string index ) {
		RemoveItem( GetItemFromIndex(index)?.Prefab.name );
	}

	private ItemEntry GetItemFromIndex(string index) {
		var itemListEntries = EntryList?.Entries;
		if ( itemListEntries != null && itemListEntries.ContainsKey( index ) ) {
			return itemListEntries[index] as ItemEntry;
		}
		return null;
	}
}
