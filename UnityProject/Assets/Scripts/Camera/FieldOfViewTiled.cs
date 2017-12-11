using System;
using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Events;

public enum ManagerState
{
    Idle,
    Thread,
    Main
}

public struct ShroudAction
{
    public bool isRayCastAction;
    public Vector2 endPos;
    public Vector2 key;
    public Vector2 offset;
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

    public readonly static Queue<Action> ExecuteOnMainThread = new Queue<Action>();
    private readonly static Queue<ShroudAction> shroudStatusQueue = new Queue<ShroudAction>();
    public ManagerState State = ManagerState.Idle;
    public List<Vector2> nearbyShrouds = new List<Vector2>();
    bool updateFov = false;
    Vector3 sourcePosCache;
    LayerMask _layerMask;

    //Enumerator cache
    private static WaitForSeconds _waitForTick;

    private static WaitForSeconds _waitForHalfTick;
    private static WaitForSecondsRealtime _waitForSendDelay;
    private static WaitForEndOfFrame _waitForEndOfFrame;

    void Start()
    {
        if (GameData.Instance.testServer || GameData.IsHeadlessServer)
        {
            Debug.Log("Turn off FOV as this is a server");
            this.enabled = false;
            return;
        }
        _layerMask = LayerMask.GetMask("Walls", "Door Closed");
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

    void OnEnable()
    {
        EventManager.AddHandler(EVENT.UpdateFov, RecalculateFov);
    }

    void OnDisable()
    {
        EventManager.RemoveHandler(EVENT.UpdateFov, RecalculateFov);
    }

    public override void ThreadedWork()
    {
        base.ThreadedWork();
        try
        {
            State = ManagerState.Thread;
            if (updateFov)
            {
                UpdateSightSourceFov();
                updateFov = false;
            }
            State = ManagerState.Idle;
        }
        catch (Exception e)
        {
            string msg = "Exception: " + e.Message + "\n";
            foreach (var s in e.StackTrace)
            {
                msg += s;
            }
            Debug.LogError(msg);
            Debug.Log("<color=red><b>FOV Exception.</b> " + e.Message + "</color>");
            // to call unity main thread specific actions
            updateFov = false;
            ExecuteOnMainThread.Enqueue(() => { StopManager(); });
        }
    }

    // TODO Support security cameras etc
    public Vector2 GetSightSourceDirection()
    {
        //TODO: If Camera2DFollow target is a camera then do other things (i.e get the dir of cam)
        return PlayerManager.LocalPlayerScript.playerSprites.currentDirection;
    }

    IEnumerator FovProcessing()
    {
        _waitForTick = new WaitForSeconds(TickSpeed / 1000f);
        _waitForHalfTick = new WaitForSeconds(TickSpeed / 2000f);
        _waitForSendDelay = new WaitForSecondsRealtime(0.2f);
        _waitForEndOfFrame = new WaitForEndOfFrame();

        while (true)
        {
            yield return _waitForTick;
            while (State != ManagerState.Idle)
            {
                yield return _waitForEndOfFrame;
            }
            yield return _waitForHalfTick;
            while (ExecuteOnMainThread.Count > 0)
            {
                ExecuteOnMainThread.Dequeue().Invoke();
            }
            while (shroudStatusQueue.Count > 0)
            {
                SetShroudStatus(shroudStatusQueue.Dequeue());
            }

            yield return _waitForEndOfFrame;
            nearbyShrouds.Clear();
            // Update when we move the camera and we have a valid SightSource
            if (Camera2DFollow.followControl.target == null)
                continue;

            if (transform.hasChanged && !updateFov)
            {
                transform.hasChanged = false;

                if (transform.position == lastPosition && GetSightSourceDirection() == lastDirection)
                    continue;

                RecalculateFov();
            }
        }
        yield return _waitForHalfTick;
    }

    //Runs on Worker Thread:
    public void UpdateSightSourceFov()
    {
        List<Vector2> inFieldOFVision = new List<Vector2>();
        // Returns all shroud nodes in field of vision
        for (int i = nearbyShrouds.Count; i-- > 0;)
        {
            var sA = new ShroudAction() { key = nearbyShrouds[i], enabled = true };
            shroudStatusQueue.Enqueue(sA);
            // Light close behind and around
            if (Vector2.Distance(sourcePosCache, nearbyShrouds[i]) < InnatePreyVision)
            {
                inFieldOFVision.Add(nearbyShrouds[i]);
                continue;
            }

            // In front cone
            if (Vector3.Angle(new Vector3(nearbyShrouds[i].x, nearbyShrouds[i].y, 0f) - sourcePosCache, GetSightSourceDirection()) < FieldOfVision)
            {
				if (i < nearbyShrouds.Count) {
					inFieldOFVision.Add(nearbyShrouds[i]);
				}
                continue;
            }
        }

        // Loop through all tiles that are nearby and are in field of vision
        for (int i = inFieldOFVision.Count; i-- > 0;)
        {
            // There is a slight issue with linecast where objects directly diagonal to you are not hit by the cast
            // and since we are standing next to the tile we should always be able to view it, lets always deactive the shroud
            if (Vector2.Distance(inFieldOFVision[i], sourcePosCache) < 2)
            {
                var lA = new ShroudAction() { key = inFieldOFVision[i], enabled = false };
                shroudStatusQueue.Enqueue(lA);
                continue;
            }
            // Everything else:
            // Perform a linecast to see if a wall is blocking vision of the target tile
            Vector2 offsetPos = ShroudCornerOffset(Angle(((Vector2)sourcePosCache - inFieldOFVision[i]).normalized));
            var rA = new ShroudAction() { isRayCastAction = true, endPos = inFieldOFVision[i] += offsetPos, offset = offsetPos };
            shroudStatusQueue.Enqueue(rA);
        }

        inFieldOFVision = null;
    }

    float Angle(Vector2 dir)
    {
        if (dir.x < 0)
        {
            return 360 - (Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg * -1);
        }
        else
        {
            return Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        }
    }

    //Which corner should we target? Return the actual offset to be applied and cached
    Vector2 ShroudCornerOffset(float angle)
    {
        Vector2 offset = Vector2.zero;
        //TopRight
        if (angle >= 0f && angle <= 90f)
            offset = new Vector2(0.5f, 0.5f);
        // Bottom Right
        if (angle > 90f && angle <= 180f)
            offset = new Vector2(0.5f, -0.5f);
        // Bottom Left
        if (angle > 180 && angle <= 270f)
            offset = new Vector2(-0.5f, -0.5f);
        // Top Left
        if (angle > 270f && angle < 360f)
            offset = new Vector2(-0.5f, 0.5f);

        return offset;
    }

    void RayCastQueue(Vector2 endPos, Vector2 offsetPos)
    {
        RaycastHit2D hit = Physics2D.Linecast(Camera2DFollow.followControl.target.position, endPos, _layerMask);
        // If it hits a wall we should enable the shroud
        //		Debug.DrawLine(GetPlayerSource().transform.position, endPos,Color.red);
        if (hit)
        {
            float dist = Vector2.Distance(hit.point, endPos);
            if (dist > 0.5f)
            {
                // Enable shroud, a wall was in the way
                SetShroudStatus(endPos - offsetPos, true);
            }
            else
            {
                // Disable shroud, the wall was our target tile
                SetShroudStatus(endPos - offsetPos, false);
            }
        }
        else
        {
            // Vision of tile not blocked by wall, disable the shroud
            SetShroudStatus(endPos - offsetPos, false);
        }
    }

    // This function handles every queued action on the main thread, set by the WT
    private void SetShroudStatus(ShroudAction shroudAction)
    {
        if (shroudAction.isRayCastAction)
        {
            RayCastQueue(shroudAction.endPos, shroudAction.offset);
        }
        else if (shroudTiles.ContainsKey(shroudAction.key))
        {
            shroudTiles[shroudAction.key].SendMessage("SetShroudStatus", shroudAction.enabled, SendMessageOptions.DontRequireReceiver);
        }
    }

    // Changes a shroud to on or off (calling from MT)
    private void SetShroudStatus(Vector2 vector2, bool enabled)
    {
        if (shroudTiles.ContainsKey(vector2))
            shroudTiles[vector2].SendMessage("SetShroudStatus", enabled, SendMessageOptions.DontRequireReceiver);
    }

    // Adds new shroud to our cache and marks it as enabled
    public GameObject RegisterNewShroud(Vector2 vector2, bool active)
    {
        GameObject shroudObject = EffectsFactory.Instance.SpawnShroudTile(new Vector3(vector2.x, vector2.y, 0));
        shroudTiles.Add(vector2, shroudObject);
        SetShroudStatus(vector2, active);
        return shroudObject;
    }

    public List<Vector2> GetNearbyShroudTiles()
    {
        List<Vector2> nearbyShroudTiles = new List<Vector2>();

        // Get nearby shroud tiles based on monitor radius
        for (int offsetx = -MonitorRadius; offsetx <= MonitorRadius; offsetx++)
        {
            for (int offsety = -MonitorRadius; offsety <= MonitorRadius; offsety++)
            {
                int x = (int)sourcePosCache.x + offsetx;
                int y = (int)sourcePosCache.y + offsety;

                if (!shroudTiles.ContainsKey(new Vector2(x, y)))
                    RegisterNewShroud(new Vector2(x, y), false);

                nearbyShroudTiles.Add(new Vector2(x, y));
            }
        }
        return nearbyShroudTiles;
    }

    private void RecalculateFov()
    {
        sourcePosCache = Camera2DFollow.followControl.target.position;
        nearbyShrouds = GetNearbyShroudTiles();
        updateFov = true;
        lastPosition = transform.position;
        lastDirection = GetSightSourceDirection();
    }
}