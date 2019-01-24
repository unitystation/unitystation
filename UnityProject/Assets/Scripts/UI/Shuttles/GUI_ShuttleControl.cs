using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
	string rulersColor;
	string rayColor;
	string crosshairColor;

	private void Start() {
		Trigger = Provider.GetComponent<ShuttleInteract>();
		Trigger.OnStateChange.AddListener( OnStateChange );
		
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
			
			rulersColor = this["Rulers"].Value;
			rayColor = this["RadarScanRay"].Value;
			crosshairColor = this["Crosshair"].Value;

			OnStateChange( State );
		}
	}

	private void StartNormalOperation()
	{
//			EntryList.AddItems( MapIconType.Airlock, GetObjectsOf<AirLockAnimator>( null, "AirLock" ) );
		EntryList.AddItems( MapIconType.Ship, GetObjectsOf( new HashSet<MatrixMove>( new[] {MatrixMove} ) ) );
		var stationBounds = MatrixManager.Get( 0 ).MetaTileMap.GetBounds();
		int stationRadius = (int) Mathf.Abs( stationBounds.center.x - stationBounds.xMin );
		EntryList.AddStaticItem( MapIconType.Station, stationBounds.center, stationRadius );

		EntryList.AddItems( MapIconType.Waypoint, new List<GameObject>( new[] {Waypoint} ) );

		RescanElements();

		StartRefresh();
	}
	/// <summary>
	/// Add secret emag functionality to radar
	/// </summary>
	private void AddEmagItems()
	{
		EntryList.AddItems( MapIconType.Human, GetObjectsOf<PlayerSync>() );
		EntryList.AddItems( MapIconType.Ian, GetObjectsOf<Ian>() );
		RescanElements();

		StartRefresh();
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
		
//		Logger.Log( $"Ordering travel to {Waypoint.transform.position}" );
		MatrixMove.AutopilotTo( Waypoint.transform.position );
	}

	public void HideWaypoint( bool updateImmediately = true ) { 
		Waypoint.transform.position = TransformState.HiddenPos;
		if ( updateImmediately ) {
			EntryList.UpdateExclusive( Waypoint );
		}
	}

	private bool RefreshRadar = false;
	private ShuttleInteract Trigger;
	private TabState State => Trigger.State;

	private void StartRefresh() {
		RefreshRadar = true;
//		Logger.Log( "Starting radar refresh" );
		StartCoroutine( Refresh() );
	}

	public void RefreshOnce() {
		RefreshRadar = false;
		StartCoroutine( Refresh() );
	}

	private void StopRefresh() {
//		Logger.Log( "Stopping radar refresh" );
		RefreshRadar = false;
	}

	/// <summary>
	/// Shut it down as if it has no power. Controls won't react and screen will be empty
	/// </summary>
	private void PowerOff()
	{
		EntryList.Clear();
		StopRefresh();
		TurnOnOff( false );
		RescanElements();
	}

	private IEnumerator Refresh() {
		if ( State == TabState.Off )
		{
			yield break;
		}
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


	private void OnStateChange( TabState newState )
	{
		//Important: if you get NREs out of nowhere, make sure your server code doesn't accidentally run on client as well
		if ( !IsServer )
		{
			return;
		}
		switch ( newState )
		{
			case TabState.Normal:
				PowerOff();
				StartNormalOperation();
				//Important: set values from server using SetValue and not Value
				this["OffOverlay"].SetValue = DebugTools.ColorToHex(Color.clear);
				this["Rulers"].SetValue = rulersColor;
				this["RadarScanRay"].SetValue = rayColor;
				this["Crosshair"].SetValue = crosshairColor;
				
				break;
			case TabState.Emagged:
				PowerOff();
				StartNormalOperation();
				//Remove overlay
				this["OffOverlay"].SetValue = DebugTools.ColorToHex(Color.clear);
				//Repaint radar to evil colours
				this["Rulers"].SetValue = ChangeColorHue( rulersColor, -80 );
				this["RadarScanRay"].SetValue = ChangeColorHue( rayColor, -80 );
				this["Crosshair"].SetValue = ChangeColorHue( crosshairColor, -80 );
				AddEmagItems();
				
				break;
			case TabState.Off:
				PowerOff();
				//Black screen overlay
				this["OffOverlay"].SetValue = DebugTools.ColorToHex(Color.black);
				
				break;
			default:
				return;
		}
	}

	private static string ChangeColorHue( string srcHexColour, int amount )
	{
		return DebugTools.ColorToHex( HSVUtil.ChangeColorHue( DebugTools.HexToColor( srcHexColour ), amount ) );
	}

	/// <summary>
	/// Starts or stops the shuttle.
	/// </summary>
	/// <param name="off">Toggle parameter</param>
	public void TurnOnOff( bool on ) {
		if ( on && State != TabState.Off ) {
			MatrixMove.StartMovement();
		} else {
			MatrixMove.StopMovement();
		}
	}

	/// <summary>
	/// Turns the shuttle right.
	/// </summary>
	public void TurnRight() {
		if ( State == TabState.Off )
		{
			return;
		}
		MatrixMove.TryRotate( true );
	}

	/// <summary>
	/// Turns the shuttle left.
	/// </summary>
	public void TurnLeft() {
		if ( State == TabState.Off )
		{
			return;
		}
		MatrixMove.TryRotate( false );
	}

	/// <summary>
	/// Sets shuttle speed.
	/// </summary>
	/// <param name="speedMultiplier"></param>
	public void SetSpeed( float speedMultiplier ) {
		float speed = speedMultiplier * ( MatrixMove.maxSpeed - 1 ) + 1;
//		Logger.Log( $"Multiplier={speedMultiplier}, setting speed to {speed}" );
		MatrixMove.SetSpeed( speed );
	}
}