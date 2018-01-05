using System.Collections;
using PlayGroup;
using Tilemaps;
using Tilemaps.Behaviours.Objects;
using Tilemaps.Scripts;
using UnityEngine;
using UnityEngine.Networking;

public class PushPull : VisibleBehaviour
{
	public bool allowedToMove = true;

	[SyncVar] public Vector3 currentPos;
	public bool isPushable = true;

	private Matrix matrix => registerTile.Matrix;
	private CustomNetTransform customNetTransform;

	[SyncVar] public GameObject pulledBy;

	public bool pushing;

	public Vector3 pushTarget;

	public GameObject pusher { get; private set; }

	public override void OnStartClient()
	{
		StartCoroutine(WaitForLoad());

		base.OnStartClient();
	}

	private IEnumerator WaitForLoad()
	{
		yield return new WaitForSeconds(2f);
		
		if (registerTile == null)
		{
			registerTile = GetComponent<RegisterTile>();
		}
		
		registerTile.UpdatePosition();
		
		customNetTransform = GetComponent<CustomNetTransform>();
	}

	public virtual void OnMouseDown()
	{
		// PlayerManager.LocalPlayerScript.playerMove.pushPull.pulledBy == null condition makes sure that the player itself
		// isn't being pulled. If he is then he is not allowed to pull anything else as this can cause problems
		if (Input.GetKey(KeyCode.LeftControl) && PlayerManager.LocalPlayerScript.IsInReach(transform.position) &&
		    transform != PlayerManager.LocalPlayerScript.transform && PlayerManager.LocalPlayerScript.playerMove.pushPull.pulledBy == null)
		{
			Debug.Log("Pull is turned off until doobles can fix it");
			//FIXME: Working on a fix for pull. It is turned off for time being.
			return;

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

	public void TryPush(GameObject pushedBy, Vector2 pushDir)
	{
		if (pushDir != Vector2.up && pushDir != Vector2.right && pushDir != Vector2.down && pushDir != Vector2.left)
		{
			return;
		}
		if (pushing || !isPushable || customNetTransform.isPushing
		    || customNetTransform.predictivePushing)
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

		Vector3Int newPos = Vector3Int.RoundToInt(transform.localPosition + (Vector3) pushDir);

		if (matrix.IsPassableAt(newPos) || matrix.ContainsAt(newPos, gameObject))
		{
			//Start the push on the client, then start on the server, the server then tells all other clients to start the push also
			pusher = pushedBy;
			pushTarget = newPos;
			//Start command to push on server
			if (pusher == PlayerManager.LocalPlayer)
			{
				//pushing for local player is set to true from CNT, to make sure prediction isn't overwhelmed
				customNetTransform.PushToPosition(pushTarget,PlayerManager.LocalPlayerScript.playerMove.speed, this);
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdTryPush(gameObject, transform.localPosition, pushTarget, PlayerManager.LocalPlayerScript.playerMove.speed);
			}
		}
	}


	public void BreakPull()
	{
		Debug.Log("Doobles is currently working on pull fixes");
		//FIXME: Pulling is a WIP
		return;
		PlayerScript player = PlayerManager.LocalPlayerScript;
		if (!player.playerSync) //FIXME: this doesn't exist on the client sometimes
		{
			return;
		}
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
		if(pushing){
			if(customNetTransform.predictivePushing){
				//Wait for the server to catch up to the pushtarget if predictivePushing is true
				if(currentPos == pushTarget){
					//if it is then set it to false, this ensures that the player cannot keep pushing if
					//he is experiencing high lag by waiting for the server position to match up
					customNetTransform.predictivePushing = false;
				}
			}

			if(transform.localPosition == pushTarget && !customNetTransform.isPushing && !customNetTransform.predictivePushing){
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
}