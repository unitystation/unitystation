using UnityEngine;
using Util;

public class GUI_Spawner : NetUITab 
{
	public void AddItem( string item ) {
		EntryList?.AddItem( item );
	}
	public void RemoveItem( string item ) {
		EntryList?.RemoveItem( item );
	}

	public void SpawnItem( string index ) {
		var itemListEntries = EntryList?.Entries;
		if ( itemListEntries != null && itemListEntries.ContainsKey( index ) ) {
			var item = itemListEntries[index] as ItemEntry;
			var prefab = item?.Prefab;
			Debug.Log( $"Spawning item '{prefab?.name}'!" );
			Vector3 originPos = Provider.WorldPos();
			var spawnedItem = ItemFactory.SpawnItem( prefab, originPos );
			spawnedItem.GetComponent<CustomNetTransform>()?.Throw( new ThrowInfo {
				ThrownBy = Provider,
				Aim = BodyPartType.CHEST,
				OriginPos	= originPos + Vector3.down/2,
				TargetPos = originPos + Vector3.down * 10,
				SpinMode = SpinMode.CounterClockwise
			} );
		} else {
			//todo spew error
		}
	}
	private ItemList EntryList => Info["EntryList"] as ItemList;
	private void Start() {
		//Not doing this for clients
		if ( CustomNetworkManager.Instance._isServer ) {
//			Debug.Log( $"{name} Kinda init. Nuke code is {NukeInteract.NukeCode}" );
//			InitialInfoText = $"Enter {NukeInteract.NukeCode.ToString().Length}-digit code:";
//			InfoDisplay.SetValue = InitialInfoText;
		}
	}
}
