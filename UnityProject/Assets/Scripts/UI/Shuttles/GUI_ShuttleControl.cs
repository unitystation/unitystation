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

	private GameObject Waypoint;

	private void Start() {
		//Not doing this for clients
		if ( IsServer ) {
			EntryList.Origin = MatrixMove;
			//Init listeners
			MatrixMove.OnStart.AddListener( () => this["StartButton"].SetValue = "1" );
			MatrixMove.OnStop.AddListener( () =>
			{
				this["StartButton"].SetValue = "0";
				HideWaypoint();
			} );

			if ( !Waypoint ) {
				Waypoint = new GameObject( $"{MatrixMove.gameObject.name}Waypoint" );
			}
			HideWaypoint(false);
			
//			EntryList.AddItems( MapIconType.Airlock, GetObjectsOf<AirLockAnimator>( null, "AirLock" ) );
			EntryList.AddItems( MapIconType.Ship, GetObjectsOf( new HashSet<MatrixMove>( new[] {MatrixMove} ) ) );
			var stationBounds = MatrixManager.Get( 0 ).MetaTileMap.GetBounds();
			int stationRadius = (int)Mathf.Abs(stationBounds.center.x - stationBounds.xMin);
			EntryList.AddStaticItem( MapIconType.Station, stationBounds.center, stationRadius );
			
			EntryList.AddItems( MapIconType.Waypoint, new List<GameObject>(new[]{Waypoint}) );
			
			RescanElements();

			StartRefresh();
		}
	}

	private bool Autopilot = true;
	public void SetAutopilot( bool on ) {
		Autopilot = on;
		if ( on ) {
			//touchscreen on
		} else {
			//touchscreen off, hide waypoint, invalidate MM target
			HideWaypoint();
			MatrixMove.DisableAutopilotTarget();
		}
	}
	
	public void SetSafetyProtocols( bool on ) {
		MatrixMove.SafetyProtocolsOn = on;
	}

	public void SetWaypoint( string position ) 
	{
		if ( !Autopilot ) {
			return;
		}
		Vector3 proposedPos = position.Vectorized();
		if ( proposedPos == TransformState.HiddenPos ) {
			return;
		}
		
		//Ignoring requests to set waypoint outside intended radar window
		if ( RadarList.ProjectionMagnitude( proposedPos ) > EntryList.Range ) {
			return;
		}
		//Mind the ship's actual position
		Waypoint.transform.position = (Vector2) proposedPos + Vector2Int.RoundToInt(MatrixMove.State.Position);
		
		EntryList.UpdateExclusive( Waypoint );
		
//		Debug.Log( $"Ordering travel to {Waypoint.transform.position}" );
		MatrixMove.AutopilotTo( Waypoint.transform.position );
	}

	public void HideWaypoint( bool updateImmediately = true ) { 
		Waypoint.transform.position = TransformState.HiddenPos;
		if ( updateImmediately ) {
			EntryList.UpdateExclusive( Waypoint );
		}
	}

	private bool RefreshRadar = false;

	private void StartRefresh() {
		RefreshRadar = true;
//		Debug.Log( "Starting radar refresh" );
		StartCoroutine( Refresh() );
	}

	public void RefreshOnce() {
		RefreshRadar = false;
		StartCoroutine( Refresh() );
	}

	private void StopRefresh() {
//		Debug.Log( "Stopping radar refresh" );
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