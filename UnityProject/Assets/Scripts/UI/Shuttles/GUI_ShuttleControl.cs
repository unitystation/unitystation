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
		//Not doing this for clients, but serverplayer does this too, so be aware
		if ( CustomNetworkManager.Instance._isServer ) {
			//protection against serverplayer doing the same
			if ( EntryList.Entries.Count > 0 ) {
				return;
			}
			EntryList.AddItems( MapIconType.Airlock, MatrixMove.gameObject, GetObjectsOf<AirLockAnimator>( MatrixMove.State.Position, null, "AirLock" ) );
			EntryList.AddItems( MapIconType.Ship, MatrixMove.gameObject, GetObjectsOf( MatrixMove.State.Position, new HashSet<MatrixMove>( new[] {MatrixMove})) );
			EntryList.AddItems( MapIconType.Station, MatrixMove.gameObject, new List<GameObject> {MatrixManager.Get( 0 ).GameObject} );
			refreshing = true;
			StartCoroutine( Refresh() );
		}
	}

	private bool refreshing = false;

	private IEnumerator Refresh() {
		EntryList.RefreshTrackedPos();
		yield return new WaitForSeconds( 1.5f );

		if ( refreshing ) {
			StartCoroutine( Refresh() );
		}
	}

	/// Get a list of positions for objects of given type within certain range from provided origin
	private List<GameObject> GetObjectsOf<T>( Vector3 originPos, HashSet<T> except = null, string nameFilter="", int maxRange = 200 ) 
		where T : Behaviour 
	{
		
		T[] foundBehaviours = FindObjectsOfType<T>();
		var objectsInRange = new List<GameObject>();
		
		for ( var i = 0; i < foundBehaviours.Length; i++ ) 
		{
			if ( except != null && except.Contains(foundBehaviours[i]) ) {
				continue;
			}
			var foundObject = foundBehaviours[i].gameObject;
			if ( nameFilter != "" && !foundObject.name.Contains( nameFilter ) ) {
				continue;
			}

			Vector3 pos = foundObject.WorldPos();
			if ( Vector2.Distance( originPos, pos ) <= maxRange ) {
				objectsInRange.Add( foundObject );
			}
		}

		return objectsInRange;
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