using System.Collections;
using System.Collections.Generic;
using Doors;
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
		//Not doing this for clients
		if ( IsServer ) {
			EntryList.Origin = MatrixMove;

//			EntryList.AddItems( MapIconType.Airlock, GetObjectsOf<AirLockAnimator>( null, "AirLock" ) );
			EntryList.AddItems( MapIconType.Ship, GetObjectsOf( new HashSet<MatrixMove>( new[] {MatrixMove} ) ) );
			var stationBounds = MatrixManager.Get( 0 ).MetaTileMap.GetBounds();
			int stationRadius = (int)Mathf.Abs(stationBounds.center.x - stationBounds.xMin);
			EntryList.AddStaticItem( MapIconType.Station, stationBounds.center, stationRadius );
			StartRefresh();
		}
	}

	public void SetWaypoint( string position ) {
		var pos = position.Vectorized();
		//todo modify existing one instead of creating new ones
		EntryList.AddStaticItem( MapIconType.Waypoint, pos ); 
	}

	private bool RefreshRadar = false;

	private void StartRefresh() {
		RefreshRadar = true;
		Debug.Log( "Starting radar refresh" );
		StartCoroutine( Refresh() );
	}

	public void RefreshOnce() {
		RefreshRadar = false;
		StartCoroutine( Refresh() );
	}

	private void StopRefresh() {
		Debug.Log( "Stopping radar refresh" );
		RefreshRadar = false;
	}

	private IEnumerator Refresh() {
		EntryList.RefreshTrackedPos();
		yield return new WaitForSeconds( 2f );

		if ( RefreshRadar ) {
			StartCoroutine( Refresh() );
		}
	}

	/// Get a list of positions for objects of given type within certain range from provided origin
	private List<GameObject> GetObjectsOf<T>( HashSet<T> except = null, string nameFilter="" ) 
		where T : Behaviour 
	{
		T[] foundBehaviours = FindObjectsOfType<T>();
		var foundObjects = new List<GameObject>();
		
		for ( var i = 0; i < foundBehaviours.Length; i++ ) 
		{
			if ( except != null && except.Contains(foundBehaviours[i]) ) {
				continue;
			}
			var foundObject = foundBehaviours[i].gameObject;
			if ( nameFilter != "" && !foundObject.name.Contains( nameFilter ) ) {
				continue;
			}

			foundObjects.Add( foundObject );
		}

		return foundObjects;
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
//		Debug.Log( $"Multiplier={speedMultiplier}, setting speed to {speed}" );
		MatrixMove.SetSpeed( speed );
	}
}