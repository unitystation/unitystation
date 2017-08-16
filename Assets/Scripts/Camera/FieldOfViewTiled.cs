using Matrix;
using System;
using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ManagerState
{
	Idle,
	Thread,
	Main
}

//What is the scene camera following
public enum CurrentSource
{
	player,
	brigCamera,
	medbayCamera
	//etc
}

public struct ShroudAction
{
	public bool isRayCastAction;
	public Vector2 endPos;
	public Vector2 key;
	public bool enabled;
}

public class FieldOfViewTiled : ThreadedBehaviour
{
	public int MonitorRadius = 12;
	public int FieldOfVision = 90;
	public int InnatePreyVision = 6;
	public Dictionary<Vector2, GameObject> shroudTiles = new Dictionary<Vector2, GameObject>(new Vector2EqualityComparer());
	private Vector3 lastPosition;
	private Vector2 lastDirection;
	public int WallLayer = 9;

	public readonly static Queue<Action> ExecuteOnMainThread = new Queue<Action>();
	private readonly static Queue<ShroudAction> shroudStatusQueue = new Queue<ShroudAction>();
	public ManagerState State = ManagerState.Idle;
	public List<Vector2> nearbyShrouds = new List<Vector2>();
	bool updateFov = false;
	CurrentSource currentSource;
	Transform sourceCache;
	Vector3 sourcePosCache;
	LayerMask _layerMask;

	void Start()
	{
		_layerMask = LayerMask.GetMask("Walls", "Door Closed");
		currentSource = CurrentSource.player;
		StartManager();
	}

	public override void StartManager()
	{
		base.StartManager();
		State = ManagerState.Idle;
		StartCoroutine(FovProcessing());
	}

	public override void StopManager()
	{
		base.StopManager();
		State = ManagerState.Idle;
		StopCoroutine(FovProcessing());
		Debug.Log("STOP FOV THREAD");

	}

	public override void ThreadedWork()
	{
		base.ThreadedWork();
		try {
			State = ManagerState.Thread;
			if (updateFov) {
				UpdateSightSourceFov();
				updateFov = false;
			}
			State = ManagerState.Idle;

		} catch (Exception e) {
			string msg = "Exception: " + e.Message + "\n";
			foreach (var s in e.StackTrace) {
				msg += s;
			}
			Debug.LogError(msg);
			Debug.Log("<color=red><b>FOV Exception.</b> " + e.Message + "</color>");
			// to call unity main thread specific actions
			updateFov = false;
			ExecuteOnMainThread.Enqueue(() => {  
				StopManager();
			});
		}
	}
        
	// This should return the current GameObject which is providing vision
	// into the fog of war - such as a security camera or a player
	public Transform GetPlayerSource()
	{
		// TODO Support security cameras etc
		return PlayerManager.LocalPlayer.transform;
	}

	// TODO Support security cameras etc
	public Vector2 GetSightSourceDirection()
	{
		return PlayerManager.LocalPlayerScript.playerSprites.currentDirection;
	}

	private static WaitForSeconds _waitForTick;
	private static WaitForSeconds _waitForHalfTick;
	private static WaitForSecondsRealtime _waitForSendDelay;
	private static WaitForEndOfFrame _waitForEndOfFrame;

	IEnumerator FovProcessing()
	{
		_waitForTick = new WaitForSeconds(TickSpeed / 1000f);
		_waitForHalfTick = new WaitForSeconds(TickSpeed / 2000f);
		_waitForSendDelay = new WaitForSecondsRealtime(0.2f);
		_waitForEndOfFrame = new WaitForEndOfFrame();

		while (true) {
			yield return _waitForTick;
			while (State != ManagerState.Idle) {
				yield return _waitForEndOfFrame;
			}
			yield return _waitForHalfTick;
			while (ExecuteOnMainThread.Count > 0) {
				ExecuteOnMainThread.Dequeue().Invoke();
			}
			while (shroudStatusQueue.Count > 0) {
				SetShroudStatus(shroudStatusQueue.Dequeue());
			}

			yield return _waitForEndOfFrame;
			nearbyShrouds.Clear();
			// Update when we move the camera and we have a valid SightSource
			if (sourceCache == null)
				continue;

			if (transform.hasChanged && !updateFov) {
				transform.hasChanged = false;

				if (transform.position == lastPosition && GetSightSourceDirection() == lastDirection)
					continue;

				nearbyShrouds = GetNearbyShroudTiles();
				sourcePosCache = GetPlayerSource().transform.position;
				updateFov = true;
				lastPosition = transform.position;
				lastDirection = GetSightSourceDirection();
			}

		}
		yield return _waitForHalfTick;
	}

	// Update is called once per frame
	public void Update()
	{      
		if (PlayerManager.LocalPlayer != null) {
			if (currentSource == CurrentSource.player && sourceCache != PlayerManager.LocalPlayer.transform) {
				sourceCache = GetPlayerSource();
			}
		}
	}

    //Worker Thread:
	public void UpdateSightSourceFov()
	{
		List<Vector2> inFieldOFVision = new List<Vector2>();
		// Returns all shroud nodes in field of vision
		for (int i = nearbyShrouds.Count; i-- > 0;) {
			var j = i;
			var sA = new ShroudAction(){ key = nearbyShrouds[j], enabled = true };
			shroudStatusQueue.Enqueue(sA);
			// Light close behind and around
			if (Vector2.Distance(sourcePosCache, nearbyShrouds[i]) < InnatePreyVision) {
				inFieldOFVision.Add(nearbyShrouds[j]);
				continue;
			}

			// In front cone
			if (Vector3.Angle(new Vector3(nearbyShrouds[i].x, nearbyShrouds[i].y, 0f) - sourcePosCache, GetSightSourceDirection()) < FieldOfVision) {
				inFieldOFVision.Add(nearbyShrouds[j]);
				continue;
			}
		}
			
		// Loop through all tiles that are nearby and are in field of vision
		for (int i = inFieldOFVision.Count; i-- > 0;) {
			// There is a slight issue with linecast where objects directly diagonal to you are not hit by the cast
			// and since we are standing next to the tile we should always be able to view it, lets always deactive the shroud
			var j = i;
			if (Vector2.Distance(inFieldOFVision[i], sourcePosCache) < 2) {
				var lA = new ShroudAction(){ key = inFieldOFVision[j], enabled = false };
				shroudStatusQueue.Enqueue(lA);
				continue;
			}
			// Everything else:
			// Perform a linecast to see if a wall is blocking vision of the target tile
			var rA = new ShroudAction(){ isRayCastAction = true, endPos = inFieldOFVision[j] };
			shroudStatusQueue.Enqueue(rA);
		}	
	}

	void RayCastQueue(Vector2 endPos)
	{
		RaycastHit2D hit = Physics2D.Linecast(GetPlayerSource().transform.position, endPos, _layerMask);
		// If it hits a wall we should enable the shroud
//		Debug.DrawLine(GetPlayerSource().transform.position, endPos,Color.red);
		if (hit) {
			if (new Vector2(hit.transform.position.x, hit.transform.position.y) != endPos) {
				// Enable shroud, a wall was in the way
				SetShroudStatus(endPos, true);
			} else {
				// Disable shroud, the wall was our target tile
				SetShroudStatus(endPos, false);
			}
		} else {
			// Vision of tile not blocked by wall, disable the shroud
			SetShroudStatus(endPos, false);
		}
	}

	// Changes a shroud to on or off
	private void SetShroudStatus(ShroudAction shroudAction)
	{
		if (shroudAction.isRayCastAction) {
			RayCastQueue(shroudAction.endPos);
		} else if (shroudTiles.ContainsKey(shroudAction.key)) {
			shroudTiles[shroudAction.key].SendMessage("SetShroudStatus", shroudAction.enabled, SendMessageOptions.DontRequireReceiver);
		}
	}

	private void SetShroudStatus(Vector2 vector2, bool enabled)
	{
		if (shroudTiles.ContainsKey(vector2))
			shroudTiles[vector2].SendMessage("SetShroudStatus", enabled, SendMessageOptions.DontRequireReceiver);
	}
	// Adds new shroud to our cache and marks it as enabled
	public GameObject RegisterNewShroud(Vector2 vector2, bool active)
	{
		GameObject shroudObject = ItemFactory.Instance.SpawnShroudTile(new Vector3(vector2.x, vector2.y, 0));
		shroudTiles.Add(vector2, shroudObject);
		SetShroudStatus(vector2, active);
		return shroudObject;
	}

	public List<Vector2> GetNearbyShroudTiles()
	{
		List<Vector2> nearbyShroudTiles = new List<Vector2>();

		// Get nearby shroud tiles based on monitor radius
		for (int offsetx = -MonitorRadius; offsetx <= MonitorRadius; offsetx++) {
			for (int offsety = -MonitorRadius; offsety <= MonitorRadius; offsety++) {
				int x = (int)GetPlayerSource().transform.position.x + offsetx;
				int y = (int)GetPlayerSource().transform.position.y + offsety;

				// TODO Registration should probably be moved elsewhere
				Matrix.MatrixNode node = Matrix.Matrix.At(new Vector2(x, y));
				if (!shroudTiles.ContainsKey(new Vector2(x, y)))
				if (node.IsSpace() || node.IsWall() || node.IsDoor() || node.IsWindow())
					continue;

				if (!shroudTiles.ContainsKey(new Vector2(x, y)))
					RegisterNewShroud(new Vector2(x, y), false);

				nearbyShroudTiles.Add(new Vector2(x, y));
			}
		}
		return nearbyShroudTiles;
	}
}