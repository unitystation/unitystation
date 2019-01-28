using UnityEngine;
using UnityEngine.UI;

public class ControlAction : MonoBehaviour
{
	public Image throwImage;
	public Sprite[] throwSprites;

	public Image pullImage;

	private void Start()
	{
		UIManager.IsThrow = false;

		pullImage.enabled = false;
	}

	/*
	 * Button OnClick methods
	 */

	/// <summary>
	/// Perform the resist action
	/// </summary>
	public void Resist()
	{
		// TODO implement resist functionality once handcuffs and things are in
		SoundManager.Play("Click01");
		Logger.Log("Resist Button", Category.UI);
	}

	/// <summary>
	/// Perform the drop action
	/// </summary>
	public void Drop()
	{
		PlayerScript lps = PlayerManager.LocalPlayerScript;
		if (!lps || lps.canNotInteract())
		{
			return;
		}
		UI_ItemSlot currentSlot = UIManager.Hands.CurrentSlot;
		//			Vector3 dropPos = lps.gameObject.transform.position;
		if (!currentSlot.CanPlaceItem())
		{
			return;
		}
		//            if ( isNotMovingClient(lps) )
		//            {
		//               // Full client simulation(standing still)
		//                var placedOk = currentSlot.PlaceItem(dropPos);
		//                if ( !placedOk )
		//                {
		//                    Logger.Log("Client dropping error");
		//                }
		//            }
		//            else
		//            {
		//Only clear slot(while moving, as prediction is shit in this situation)
		//			GameObject dropObj = currentSlot.Item;
		//			CustomNetTransform cnt = dropObj.GetComponent<CustomNetTransform>();
		//			It is converted to LocalPos in transformstate struct
		//			cnt.AppearAtPosition(PlayerManager.LocalPlayer.transform.position);
		//            }
		//Message

		if(UIManager.IsThrow == true)
		{
			Throw(); // Disable throw
		}
		UIManager.CheckStorageHandlerOnMove(currentSlot.Item);
		lps.playerNetworkActions.RequestDropItem(currentSlot.inventorySlot.UUID, false);
		SoundManager.Play("Click01");
		Logger.Log("Drop Button", Category.UI);
	}

	// private static bool isNotMovingClient(PlayerScript lps)
	// {
	// 	return !lps.isServer && !lps.playerMove.isMoving;
	// }

	/// <summary>
	/// Throw mode toggle. Actual throw is in <see cref="MouseInputController.CheckThrow()"/>
	/// </summary>
	public void Throw(bool forceDisable = false)
	{
		if (forceDisable)
		{
			Logger.Log("Throw force disabled", Category.UI);
			UIManager.IsThrow = false;
			throwImage.sprite = throwSprites[0];
			return;
		}

		// See if requesting to enable or disable throw
		if (throwImage.sprite == throwSprites[0] && UIManager.IsThrow == false)
		{
			PlayerScript lps = PlayerManager.LocalPlayerScript;
			UI_ItemSlot currentSlot = UIManager.Hands.CurrentSlot;

			// Check if player can throw
			if (!lps || lps.canNotInteract())
			{
				return;
			}

			// Enable throw
			Logger.Log("Throw Button Enabled", Category.UI);
			SoundManager.Play("Click01");
			UIManager.IsThrow = true;
			throwImage.sprite = throwSprites[1];
		}
		else if (throwImage.sprite == throwSprites[1] && UIManager.IsThrow == true)
		{
			// Disable throw
			Logger.Log("Throw Button Disabled", Category.UI);
			UIManager.IsThrow = false;
			throwImage.sprite = throwSprites[0];
		}
	}

	/// <summary>
	/// Stops pulling whatever we're pulling
	/// </summary>
	public void StopPulling()
	{
		if (pullImage && pullImage.enabled)
		{
			PlayerScript ps = PlayerManager.LocalPlayerScript;

			ps.pushPull.CmdStopPulling();
		}
	}

	/// <summary>
	/// Updates whether or not the "Stop Pulling" button is shown
	/// </summary>
	/// <param name="show">Whether or not to show the button</param>
	public void UpdatePullingUI(bool show)
	{
		pullImage.enabled = show;
	}
}