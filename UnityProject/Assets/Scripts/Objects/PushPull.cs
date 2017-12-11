using System.Collections;
using PlayGroup;
using Tilemaps.Scripts.Behaviours.Objects;
using UnityEngine;
using UnityEngine.Networking;
using Matrix = Tilemaps.Scripts.Matrix;

public class PushPull : VisibleBehaviour
{
    public float moveSpeed = 7f;
    public bool allowedToMove = true;
    public bool isPushable = true;

    [SyncVar] public GameObject pulledBy;

    //cache
    private float journeyLength;

    public Vector3 pushTarget;

    public GameObject pusher { get; private set; }

    public bool pushing = false;
    public bool serverLittleLag = false;

    [SyncVar(hook = "PushSync")] public Vector3 serverPos;

    [SyncVar] public Vector3 currentPos;

    private Matrix matrix;

    //A check to make sure there are no network errors
    public float timeInPush = 0f;

    public override void OnStartClient()
    {
        StartCoroutine(WaitForLoad());

        base.OnStartClient();
    }

    IEnumerator WaitForLoad()
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
        else if (pulledBy != PlayerManager.LocalPlayer)
        {
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
        var newPos = Vector3Int.RoundToInt(transform.localPosition + (Vector3)pushDir);
        //newPos.z = transform.localPosition.z;


        if (matrix.IsPassableAt(newPos) || matrix.ContainsAt(newPos, gameObject)) 
        {
            //Start the push on the client, then start on the server, the server then tells all other clients to start the push also
            pusher = pushedBy;
            if (pusher == PlayerManager.LocalPlayer)
                PlayerManager.LocalPlayerScript.playerMove.IsPushing = true;

            pushTarget = newPos;
            journeyLength = Vector3.Distance(transform.localPosition, newPos) + 0.2f;
            timeInPush = 0f;
            pushing = true;
            //Start command to push on server
            if (pusher == PlayerManager.LocalPlayer)
                PlayerManager.LocalPlayerScript.playerNetworkActions.CmdTryPush(gameObject, pushTarget);
        }
    }


    public void BreakPull()
    {
        var player = PlayerManager.LocalPlayerScript;
        var pullingObject = player.playerSync.pullingObject;
        if (!pullingObject || !pullingObject.Equals(gameObject)) return;
        player.playerSync.PullReset(NetworkInstanceId.Invalid);
        player.playerSync.pullingObject = null;
        player.playerSync.pullObjectID = NetworkInstanceId.Invalid;
        player.playerNetworkActions.isPulling = false;
        pulledBy = null;
    }


	void Update()
    {
        if (pushing && transform.localPosition != pushTarget)
        {
            PushTowards();
        }
        //This is a back up incase things go wrong as playerMove is important
        if (pushing)
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

	void LateUpdate()
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
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, pushTarget, (moveSpeed * Time.deltaTime) * journeyLength);

        if (transform.localPosition == pushTarget)
        {
            registerTile.UpdatePosition();

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
                PlayerManager.LocalPlayerScript.playerMove.IsPushing = false;
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
        if (transform.localPosition == pos && pusher == PlayerManager.LocalPlayer)
        {
            PlayerManager.LocalPlayerScript.playerMove.IsPushing = false;
            pusher = null;
            return;
        }
        pushTarget = pos;
        journeyLength = Vector3.Distance(transform.localPosition, pos) + 0.2f;
        timeInPush = 0f;
        pushing = true;
    }
}