using System.Collections;
using PlayGroup;
using Tilemaps.Scripts;
using Tilemaps.Scripts.Behaviours.Objects;
using UnityEngine;
using UnityEngine.Networking;

public class PushPull : VisibleBehaviour
{
    public bool allowedToMove = true;

    [SyncVar] public Vector3 currentPos;
    public bool isPushable = true;

    //cache
    private float journeyLength;

    private Matrix matrix;
    public float moveSpeed = 7f;

    [SyncVar] public GameObject pulledBy;

    public bool pushing;

    public Vector3 pushTarget;
    private Vector3 pushFrom;
    private float pushStep;
    public bool serverLittleLag;

    //A check to make sure there are no network errors
    public float timeInPush;

    public GameObject pusher { get; private set; }

    public override void OnStartClient()
    {
        StartCoroutine(WaitForLoad());

        base.OnStartClient();
    }

    private IEnumerator WaitForLoad()
    {
        yield return new WaitForSeconds(2f);
//        if (currentPos != Vector3.zero)
//        {
        if (registerTile == null)
        {
            registerTile = GetComponent<RegisterTile>();
        }
//            transform.localPosition = RoundedPos(currentPos);
        registerTile.UpdatePosition();
        matrix = Matrix.GetMatrix(this);
//        }
    }

    public virtual void OnMouseDown()
    {
        // PlayerManager.LocalPlayerScript.playerMove.pushPull.pulledBy == null condition makes sure that the player itself
        // isn't being pulled. If he is then he is not allowed to pull anything else as this can cause problems
        if (Input.GetKey(KeyCode.LeftControl) && PlayerManager.LocalPlayerScript.IsInReach(transform.position)
            && transform != PlayerManager.LocalPlayerScript.transform
            && PlayerManager.LocalPlayerScript.playerMove.pushPull.pulledBy == null)
        {
            if (pulledBy == PlayerManager.LocalPlayer)
            {
                CancelPullBehaviour();
            }
            else
            {
                CancelPullBehaviour();
                PlayerManager.LocalPlayerScript.playerNetworkActions.CmdPullObject(gameObject);
            }
        }
    }

    public void CancelPullBehaviour()
    {
        if (pulledBy == PlayerManager.LocalPlayer)
        {
            PlayerManager.LocalPlayerScript.playerNetworkActions.CmdStopPulling(gameObject);
            return;
        }
        if (pulledBy != PlayerManager.LocalPlayer)
        {
            PlayerManager.LocalPlayerScript.playerNetworkActions.CmdStopOtherPulling(gameObject);
        }
        PlayerManager.LocalPlayerScript.playerSync.PullReset(gameObject.GetComponent<NetworkIdentity>().netId);
    }

    public void TryPush(GameObject pushedBy, float pusherSpeed, Vector2 pushDir)
    {
        if (pushDir != Vector2.up && pushDir != Vector2.right
            && pushDir != Vector2.down && pushDir != Vector2.left)
        {
            return;
        }
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
        Vector3Int newPos = Vector3Int.RoundToInt(transform.localPosition + (Vector3) pushDir);
        //newPos.z = transform.localPosition.z;


        if (matrix.IsPassableAt(newPos) || matrix.ContainsAt(newPos, gameObject))
        {
            //Start the push on the client, then start on the server, the server then tells all other clients to start the push also
            pusher = pushedBy;
            if (pusher == PlayerManager.LocalPlayer)
            {
                PlayerManager.LocalPlayerScript.playerMove.IsPushing = true;
            }

            pushTarget = newPos;
            pushFrom = transform.localPosition;
            pushStep = 0f;
            journeyLength = Vector3.Distance(transform.localPosition, newPos) + 0.2f;
            timeInPush = 0f;
            pushing = true;
            //Start command to push on server
            if (pusher == PlayerManager.LocalPlayer)
            {
                PlayerManager.LocalPlayerScript.playerNetworkActions.CmdTryPush(gameObject, transform.localPosition,pushTarget);
            }
        }
    }


    public void BreakPull()
    {
        PlayerScript player = PlayerManager.LocalPlayerScript;
        GameObject pullingObject = player.playerSync.pullingObject;
        if (!pullingObject || !pullingObject.Equals(gameObject))
        {
            return;
        }
        player.playerSync.PullReset(NetworkInstanceId.Invalid);
        player.playerSync.pullingObject = null;
        player.playerSync.pullObjectID = NetworkInstanceId.Invalid;
        player.playerNetworkActions.isPulling = false;
        pulledBy = null;
    }


    private void Update()
    {
        if (pushing && transform.localPosition != pushTarget)
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
                    PlayerManager.LocalPlayerScript.playerMove.IsPushing = false;
                }
                pusher = null;
                serverLittleLag = false;
                pushing = false;
            }
        }
    }

    private void LateUpdate()
    {
        if (CustomNetworkManager.Instance._isServer)
        {
            if (transform.hasChanged)
            {
                transform.hasChanged = false;
                currentPos = transform.localPosition;
            }
        }
    }

    private void PushTowards()
    {
        pushStep += ((Time.deltaTime * moveSpeed) / journeyLength);
        transform.localPosition = Vector3.Lerp(pushFrom, pushTarget, pushStep);

        if (transform.localPosition == pushTarget)
        {
            registerTile.UpdatePosition();
            if (pusher == PlayerManager.LocalPlayer)
            {
                StartCoroutine(PushFinishWait());   
            }
            pushing = false;
        }
    }

    private IEnumerator PushFinishWait()
    {
        yield return new WaitForSeconds(0.05f);
        if (serverLittleLag)
        {
            pusher = null;
            PlayerManager.LocalPlayerScript.playerMove.IsPushing = false;
        }
        else
        {
            //Server is a bit behind, wait before allowing to push again
            //TODO: Determine how far behind the server actual is and use that to wait
            yield return new WaitForSeconds(0.4f);
            pusher = null;
            PlayerManager.LocalPlayerScript.playerMove.IsPushing = false;
        }

    }
    
    [ClientRpc]
    public void RpcPushSync(Vector3 startLocalPos, Vector3 targetPos)
    {
        if (pusher == PlayerManager.LocalPlayer || transform.localPosition == targetPos)
        {
            //Rpc happened on the pushee before he was finished
            //this means there is very little lag so allow him to move straight away after it's done
            if (pushing)
            {
                serverLittleLag = true;
            }
            //No need to push if it is already at the position or is the one initiating the push
            return;
        }

        pushFrom = startLocalPos;
        pushTarget = targetPos;
        pushStep = 0f;
        journeyLength = Vector3.Distance(transform.localPosition, targetPos) + 0.2f;
        timeInPush = 0f;
        pushing = true;
    }
}