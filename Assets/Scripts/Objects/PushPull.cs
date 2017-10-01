﻿using System.Collections;
using Matrix;
using PlayGroup;
using UnityEngine;
using UnityEngine.Networking;

public class PushPull : VisibleBehaviour
{
    public float moveSpeed = 7f;
    public bool allowedToMove = true;
    public bool isPushable = true;

    [SyncVar]
    public GameObject pulledBy;
 
    //cache
    private float journeyLength;
    public Vector3 pushTarget;

    public GameObject pusher { get; private set; }

    public bool pushing = false;
    public bool serverLittleLag = false;

    [SyncVar(hook = "PushSync")]
    public Vector3 serverPos;

    [SyncVar]
	public Vector3 currentPos;

    //A check to make sure there are no network errors
    public float timeInPush = 0f;

    public override void OnStartClient()
    {
		StartCoroutine(WaitForLoad());
        base.OnStartClient();
    }
	IEnumerator WaitForLoad(){
		yield return new WaitForSeconds(2f);
		if (currentPos != Vector3.zero)
		{
			if (registerTile == null)
			{
				registerTile = GetComponent<RegisterTile>();
			}
			transform.position = RoundedPos(currentPos);
			registerTile.UpdateTile();
		}
	}

    public virtual void OnMouseDown()
    {
		if (Input.GetKey(KeyCode.LeftControl) && PlayerManager.LocalPlayerScript.IsInReach(transform)
			&& transform != PlayerManager.LocalPlayerScript.transform)
        {
			CancelPullBehaviour();
            PlayerManager.LocalPlayerScript.playerNetworkActions.CmdPullObject(gameObject);
        }
    }

	public void CancelPullBehaviour(){
		if (pulledBy == PlayerManager.LocalPlayer) {
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdStopPulling(gameObject);
			return;
		} else if (pulledBy != PlayerManager.LocalPlayer) {
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdStopOtherPulling(gameObject);
		}
		PlayerManager.LocalPlayerScript.playerSync.PullReset(gameObject.GetComponent<NetworkIdentity>().netId);
	}

    public void TryPush(GameObject pushedBy, float pusherSpeed, Vector2 pushDir)
    {
        if (pushDir != Vector2.up && pushDir != Vector2.right
        && pushDir != Vector2.down && pushDir != Vector2.left)
            return;
        if (pushing || !isPushable)
        {
            return;
        }

        if (pulledBy != null)
        {
            if (CustomNetworkManager.Instance._isServer)
            {
                pulledBy.GetComponent<PlayerNetworkActions>().CmdStopPulling(gameObject);
            }
            else
            {
                if (pulledBy == PlayerManager.LocalPlayer)
                {
                    PlayerManager.LocalPlayerScript.playerNetworkActions.isPulling = false;
                    PlayerManager.LocalPlayerScript.playerSync.pullingObject = null;
                }

                pulledBy = null;
            }
        }

        moveSpeed = pusherSpeed;
        Vector3 newPos = RoundedPos(transform.position) + (Vector3)pushDir;
        newPos.z = transform.position.z;
        if (Matrix.Matrix.At(newPos).IsPassable() || Matrix.Matrix.At(newPos).ContainsTile(gameObject))
        {
            //Start the push on the client, then start on the server, the server then tells all other clients to start the push also
            pusher = pushedBy;
            if(pusher == PlayerManager.LocalPlayer)
            PlayerManager.LocalPlayerScript.playerMove.isPushing = true;
            
            pushTarget = newPos;
            journeyLength = Vector3.Distance(transform.position, newPos) + 0.2f;
            timeInPush = 0f;
            pushing = true;
            //Start command to push on server
            if(pusher == PlayerManager.LocalPlayer)
            PlayerManager.LocalPlayerScript.playerNetworkActions.CmdTryPush(gameObject, pushTarget);
        } 
    }

    
    public void BreakPull()
    {
        var player = PlayerManager.LocalPlayerScript;
        var pullingObject = player.playerSync.pullingObject;
        if ( !pullingObject || !pullingObject.Equals(gameObject) ) return;
        player.playerSync.PullReset(NetworkInstanceId.Invalid);
        player.playerSync.pullingObject = null;
        player.playerSync.pullObjectID = NetworkInstanceId.Invalid;
        player.playerNetworkActions.isPulling = false;
        pulledBy = null;
    }
    
	public override void UpdateMe()
    {
        if (pushing && transform.position != pushTarget)
        {
            PushTowards();
        }
        //This is a back up incase things go wrong as playerMove is important
        if (pusher != null)
        {
            timeInPush += Time.deltaTime;
            if (timeInPush > 3f)
            {
                if (pusher == PlayerManager.LocalPlayer)
                {
                    PlayerManager.LocalPlayerScript.playerMove.isPushing = false;
                }
                pusher = null;
                serverLittleLag = false;
                pushing = false;
            }
        }
    }

    public override void LateUpdateMe()
    {
        if (CustomNetworkManager.Instance._isServer)
        {
            if (transform.hasChanged)
            {
                transform.hasChanged = false;
                currentPos = transform.position;
            }
        }
    }

    private void PushTowards()
    {
        transform.position = Vector3.MoveTowards(transform.position, pushTarget, (moveSpeed * Time.deltaTime) * journeyLength);
	
        if (transform.position == pushTarget)
        {
            registerTile.UpdateTile(RoundedPos(pushTarget));

            StartCoroutine(PushFinishWait());
        }
    }

    IEnumerator PushFinishWait()
    {
        yield return new WaitForSeconds(0.05f);

        if (pusher == PlayerManager.LocalPlayer)
        {
            if (serverLittleLag)
            {
                serverLittleLag = false;
                PlayerManager.LocalPlayerScript.playerMove.isPushing = false;
                pusher = null;
            }
            pushing = false;
        }
        else
        {
            pushing = false;
        }
    }

    private void PushSync(Vector3 pos)
    {
        if (pushing)
        {
            if (pusher == PlayerManager.LocalPlayer)
            {
                if (pos == pushTarget)
                {
                    serverLittleLag = true;
                }
            }
            return;
        }
        if (transform.position == pos && pusher == PlayerManager.LocalPlayer)
        {
            PlayerManager.LocalPlayerScript.playerMove.isPushing = false;
            pusher = null;
            return;
        }
        pushTarget = pos;
        journeyLength = Vector3.Distance(transform.position, pos) + 0.2f;
        timeInPush = 0f;
        pushing = true;
    }

    private Vector3 RoundedPos(Vector3 pos)
    {
        return new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), pos.z);
    }
}
