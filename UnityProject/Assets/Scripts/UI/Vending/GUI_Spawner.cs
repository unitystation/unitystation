using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_Spawner : NetTab
{
	private ItemList entryList;
	private ItemList EntryList => entryList ? entryList : entryList = this["EntryList"] as ItemList;

	private NetUIElement infoDisplay;
	private NetUIElement InfoDisplay => infoDisplay ? infoDisplay : infoDisplay = this["RandomText"];

	private NetUIElement nestedPageName;
	private NetUIElement NestedPageName => nestedPageName ? nestedPageName : nestedPageName = this["NestedPageName"];

	protected override void InitServer()
	{
		//Init fields from pages/subpages that you want to access later
		//They're visible during InitServer() but might become invisible again afterwards, so get references to them now
		InfoDisplay?.Init();
		NestedPageName?.Init();
	}

	private void Start()
	{
		if ( IsServer )
		{
			//Storytelling
			tgtMode = true;
			StartCoroutine( ToggleStory(0) );

			// Add items from InitialContents list
			List<GameObject> initList = Provider.GetComponent<SpawnerInteract>().InitialContents;
			foreach ( GameObject item in initList )
			{
				EntryList.AddItem( item );
			}

	//		Done via editor in this example, but can be done via code as well, like this:
	//		NestedSwitcher.OnPageChange.AddListener( RefreshSubpageLabel );
		}
	}

	public void RefreshSubpageLabel( NetPage oldPage, NetPage newPage )
	{
		NestedPageName.SetValue = newPage.name;
	}

	private static string[] tgt = ("One day while Andy was toggling, " +
	                            "Toggle got toggled. He could no longer help himself! " +
	                            "He watched as Andy stroked his juicy kawaii toggle.").Split( ' ' );

	private bool tgtMode;
	private IEnumerator ToggleStory(int word) {
		InfoDisplay.SetValue = tgt.Wrap(word);
		yield return YieldHelper.Second;
		yield return YieldHelper.Second;
		if ( tgtMode ) {
			StartCoroutine( ToggleStory(++word) );
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

		if ( item == null )
		{
			return;
		}

		Vector3 originPos = Provider.WorldPosServer();
		Vector3 nearestPlayerPos = GetNearestPlayerPos(originPos);

		if ( nearestPlayerPos == TransformState.HiddenPos )
		{
			return;
		}

		var spawnedItem = PoolManager.PoolNetworkInstantiate( item.Prefab, originPos );
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

	private ItemEntry GetItemFromIndex(string index)
	{
		var entryCatalog = EntryList.EntryIndex;
		if ( entryCatalog.ContainsKey( index ) )
		{
			return entryCatalog[index] as ItemEntry;
		}
		Logger.LogTraceFormat( "'{0}' spawner tab: item with string index {1} not found in the list, trying to interpret as actual array index", Category.NetUI, gameObject.name, index);
		var entries = EntryList.Entries;
		if ( int.TryParse( index, out var intIndex ) && entries.Length > intIndex )
		{
			return entries[intIndex] as ItemEntry;
		}
		Logger.LogErrorFormat( "'{0}' spawner tab: item with index {1} not found in the list, might be hidden/destroyed", Category.NetUI, gameObject.name, index);
		return null;
	}
}
