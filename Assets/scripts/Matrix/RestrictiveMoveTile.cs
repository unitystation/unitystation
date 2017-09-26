using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Matrix;

/// <summary>
/// This is grabbed by the matrix to determine if player can
/// leave a tile in certain directions. Used for glass doors 
/// or climbing into a window.
/// </summary>
[ExecuteInEditMode]
public class RestrictiveMoveTile : NetworkBehaviour {

	private RestrictedMoveStruct restrictMoveStruct = new RestrictedMoveStruct();
	public RestrictedMoveStruct GetRestrictedData{ get { return restrictMoveStruct; }}

	//As stated by unity team, it is preferable to sync individual
	//values instead of syncing a whole struct.
	[SyncVar(hook = "UpdateNorth")]
	public bool restrictNorth;
	[SyncVar(hook = "UpdateSouth")]
	public bool restrictSouth;
	[SyncVar(hook = "UpdateEast")]
	public bool restrictEast;
	[SyncVar(hook = "UpdateWest")]
	public bool restrictWest;

	private RegisterTile registerTile;

	private void Start()
	{
		restrictMoveStruct.north = restrictNorth;
		restrictMoveStruct.south = restrictSouth;
		restrictMoveStruct.east = restrictEast;
		restrictMoveStruct.west = restrictWest;
	}

	public override void OnStartClient(){
		registerTile = GetComponent<RegisterTile>();
		StartCoroutine(WaitForLoad());
		base.OnStartClient();
	}

	IEnumerator WaitForLoad(){
		yield return new WaitForSeconds(2f);
		UpdateNorth(restrictNorth);
		UpdateSouth(restrictSouth);
		UpdateEast(restrictEast);
		UpdateWest(restrictWest);
	}


	private void UpdateNorth(bool val){
		restrictNorth = val;
		restrictMoveStruct.north = restrictNorth;
		registerTile.UpdateTile();
	}

	private void UpdateSouth(bool val)
	{
		restrictSouth = val;
		restrictMoveStruct.south = restrictSouth;
		registerTile.UpdateTile();
	}

	private void UpdateEast(bool val)
	{
		restrictEast = val;
		restrictMoveStruct.east = restrictEast;
		registerTile.UpdateTile();
	}

	private void UpdateWest(bool val)
	{
		restrictWest = val;
		restrictMoveStruct.west = restrictWest;
		registerTile.UpdateTile();
	}
}
