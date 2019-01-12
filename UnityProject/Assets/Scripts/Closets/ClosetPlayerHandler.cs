using UnityEngine;


//TODO make into a more generic component to handle coffins, disposal bins etc. Will
//require a rename also
/// <summary>
///     A temporary component added to the players localplayer
///     by ObjectBehaviour.cs when they are hidden in a cupboard.
///     It is also removed by ObjectBehaviour when they leave.
///     It will wait for directional key inputs and then check
///     if the player can leave.
/// </summary>
public class ClosetPlayerHandler : MonoBehaviour
{
	private ClosetControl closetControl;
	private bool monitor;

	public void Init(ClosetControl closetCtrl)
	{
		closetControl = closetCtrl;

		if (!PlayerManager.LocalPlayerScript.playerNetworkActions.isGhost)
		{
			// TODO: Change this stuff to the proper settings once re-entering corpse becomes a feature.
			Camera2DFollow.followControl.target = closetControl.transform;
			Camera2DFollow.followControl.damping = 0.2f;
		}

		if (!closetControl)
		{
			//this is not a closet. Could be a coffin or disposals
			Logger.LogWarning("No closet found for ClosetPlayerHandler!" + " maybe it's time to update this component? (see the todo's)", Category.Containers);
			Destroy(this);
		}
		else
		{
			monitor = true;
		}
	}

	private void Update()
	{
		if (PlayerManager.LocalPlayerScript.playerNetworkActions.isGhost || UIManager.IsInputFocus)
		{
			return;
		}
		if (monitor)
		{
			if (KeyboardInputManager.IsMovementPressed())
			{
				if (!closetControl.IsLocked)
				{
					//TODO: This should probably be done in the main inputcontroller rather than in Update
					closetControl.Interact(gameObject, "lefthand");
				}
			}
			//take the player with the closet so they can interact with it
			if (closetControl.transform.position != transform.position)
			{
				transform.position = closetControl.transform.position;
			}
		}
	}
}
