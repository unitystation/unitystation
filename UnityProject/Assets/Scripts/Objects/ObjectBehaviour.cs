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

	[ContextMethod("Drag","Drag_Hand")]
	public void GUIOnMouseDown()
	{
		OnMouseDown();
	}

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
						if (!PlayerManager.LocalPlayerScript.playerNetworkActions.isGhost)
						{
							Camera2DFollow.followControl.target = transform;
						}
						Camera2DFollow.followControl.damping = 0f;
						Destroy(closetHandlerCache);
					}
				}
			}
		}
	}
}