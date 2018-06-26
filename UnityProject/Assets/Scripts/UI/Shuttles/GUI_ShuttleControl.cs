using System.Collections.Generic;
using Doors;
using Tilemaps;
using UnityEngine;
using Util;

/// Server only stuff
public class GUI_ShuttleControl : NetTab {
	private RadarList entryList;
	private RadarList EntryList {
		get {
			if ( !entryList ) {
				entryList = this["EntryList"] as RadarList;
			}
			return entryList;
		} 
	}
	private MatrixMove matrixMove;
	private MatrixMove MatrixMove {
		get {
			if ( !matrixMove ) {
				matrixMove = Provider.GetComponent<ShuttleInteract>().ShuttleMatrixMove;
			}

			return matrixMove;
		}
	}
	
	private void Start() {
		//Not doing this for clients, but serverplayer does this too, so be aware
		if ( CustomNetworkManager.Instance._isServer ) {
			//testing
//			EntryList.AddItem(MapIconType.Ship, Vector2.zero);
			var airlocks = GetPositionsOf<AirLockAnimator>( MatrixMove.State.Position, null, "AirLock" );
			EntryList.AddItems( MapIconType.Airlock, airlocks.ToArray() );
			
			var ships = GetMatrixPositions( MatrixMove.State.Position, new HashSet<MatrixInfo>(new []{MatrixManager.Get( MatrixMove.gameObject )}) );
			EntryList.AddItems( MapIconType.Ship, ships.ToArray() );
			
//			var stations = GetMatrixPositions( MatrixMove.State.Position, null, false );
//			EntryList.AddItems( MapIconType.Station, stations.ToArray() );

//			EntryList.AddItem(MapIconType., Vector2.zero);
		}
	}
//	private Dictionary<MapIconType, List<Vector2>> GetRadarData(  /**/ ) {
//		var radarData = new Dictionary<MapIconType, List<Vector2>>();
//		return radarData;
//	}

	private List<Vector2> GetMatrixPositions( Vector3 originPos, ICollection<MatrixInfo> except = null, bool movable = true, int maxRange = 200 ){
		var foundPositions = new List<Vector2>();
		foreach ( var matrixInfo in MatrixManager.Instance.Matrices ) 
		{
			Vector3 matrixPos = matrixInfo.GameObject.WorldPos();
			if ( Vector2.Distance( originPos, matrixPos ) <= maxRange 
			     && movable == (matrixInfo.MatrixMove != null)
			     && (except == null || except.Contains( matrixInfo )) 
			     ) 
			{
				foundPositions.Add( matrixPos - originPos );
			}
		}
		return foundPositions;

	}

	/// Get a list of positions for objects of given type within certain range from provided origin
	private List<Vector2> GetPositionsOf<T>( Vector3 originPos, HashSet<T> except = null, string nameFilter="", int maxRange = 200 ) where T : Behaviour {
		T[] foundObjects = FindObjectsOfType<T>();
		var foundPositions = new List<Vector2>();
		
		for ( var i = 0; i < foundObjects.Length; i++ ) 
		{
			var foundObject = foundObjects[i].gameObject;
			if ( nameFilter != "" && !foundObject.name.Contains( nameFilter ) ) {
				continue;
			}

			Vector3 pos = foundObject.WorldPos();
			if ( Vector2.Distance( originPos, pos ) <= maxRange ) {
				foundPositions.Add( pos - originPos );
			}
		}

		return foundPositions;
	}

	/// <summary>
	/// Starts or stops the shuttle.
	/// </summary>
	/// <param name="off">Toggle parameter</param>
	public void TurnOnOff( bool on ) {
		if ( on ) {
			MatrixMove.StartMovement();
		} else {
			MatrixMove.StopMovement();
		}
	}

	/// <summary>
	/// Turns the shuttle right.
	/// </summary>
	public void TurnRight() {
		MatrixMove.TryRotate( true );
	}

	/// <summary>
	/// Turns the shuttle left.
	/// </summary>
	public void TurnLeft() {
		MatrixMove.TryRotate( false );
	}

	/// <summary>
	/// Sets shuttle speed.
	/// </summary>
	/// <param name="speedMultiplier"></param>
	public void SetSpeed( float speedMultiplier ) {
		float speed = speedMultiplier * ( MatrixMove.maxSpeed - 1 ) + 1;
		Debug.Log( $"Multiplier={speedMultiplier}, setting speed to {speed}" );
		MatrixMove.SetSpeed( speed );
	}
}