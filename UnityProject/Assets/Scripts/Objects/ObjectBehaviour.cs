using System.Collections;
using UnityEngine;

/// <summary>
///     Object behaviour controls all of the basic features of an object
///     like being able to hide the obj, being able to set on fire, throwing etc
/// </summary>
public class ObjectBehaviour : PushPull
{
	[HideInInspector] public ClosetPlayerHandler closetHandlerCache;

	//Inspector is controlled by ObjectBehaviourEditor
	//Please expose any properties you need in there

	public override void OnVisibilityChange(bool state)
	{
		if (registerTile.ObjectType == ObjectType.Player)
		{
			if (PlayerManager.LocalPlayerScript.gameObject == this.gameObject)
			{
				//Local player, might be in a cupboard so add a cupboard handler. The handler will remove
				//itself if not needed
				//TODO turn the ClosetPlayerHandler into a more generic component to handle disposals bin,
				//coffins etc
				if (state)
				{
					if (closetHandlerCache)
					{
						//Set the camera to follow the player again
						if (!PlayerManager.LocalPlayerScript.playerNetworkActions.isGhost) {
							StartCoroutine(TargetPlayer());
						}
						Camera2DFollow.followControl.damping = 0f;
						Destroy(closetHandlerCache);
					}
				}
			}
		}
	}
	/// Waiting until player becomes active according to PlayerSync
	/// before tracking player to avoid blinking
	private IEnumerator TargetPlayer() {
		yield return YieldHelper.EndOfFrame;
		if ( !PlayerManager.LocalPlayerScript.PlayerSync.ClientState.Active ) {
			StartCoroutine( TargetPlayer() );
		} else {
			Camera2DFollow.followControl.target = transform;
		}
	}
}