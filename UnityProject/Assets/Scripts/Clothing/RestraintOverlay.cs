using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the overlays for the handcuff sprites
/// </summary>
public class RestraintOverlay : ClothingItem, IActionGUI
{
	//TODO Different colored overlays for different restraints
	[SerializeField]
	private List<Sprite> handCuffOverlays = new List<Sprite>();

	[SerializeField] private SpriteRenderer spriteRend = null;
	private IEnumerator uncuffCoroutine;
	private float healthCache;
	private Vector3Int positionCache;

	[SerializeField]
	private ActionData actionData = null;

	public ActionData ActionData => actionData;

	public override void SetReference(GameObject Item)
	{
		GameObjectReference = Item;
		if (Item == null)
		{
			spriteRend.sprite = null;
		}
		else
		{
			spriteRend.sprite = handCuffOverlays[referenceOffset];
		}
		DetermineAlertUI();
	}

	public override void UpdateSprite()
	{
		if (GameObjectReference != null)
		{
			spriteRend.sprite = handCuffOverlays[referenceOffset];
		}
	}

	private void DetermineAlertUI()
	{
		if (thisPlayerScript != PlayerManager.PlayerScript) return;

		if (GameObjectReference != null)
		{
			UIActionManager.ToggleLocal(this, true);
		}
		else
		{
			UIActionManager.ToggleLocal(this, false);
		}
	}

	public void ServerBeginUnCuffAttempt()
	{
		if (uncuffCoroutine != null)
			StopCoroutine(uncuffCoroutine);

		float resistTime = GameObjectReference.GetComponent<Restraint>().ResistTime;
		healthCache = thisPlayerScript.playerHealth.OverallHealth;
		positionCache = thisPlayerScript.registerTile.LocalPositionServer;
		if (!CanUncuff()) return;

		uncuffCoroutine = UncuffCountDown(resistTime);
		StartCoroutine(uncuffCoroutine);
		Chat.AddActionMsgToChat(
			thisPlayerScript.gameObject,
			$"You are attempting to remove the cuffs. This takes up to {resistTime:0} seconds",
			thisPlayerScript.playerName + " is attempting to remove their cuffs");
	}

	private IEnumerator UncuffCountDown(float resistTime)
	{
		float waitTime = 0f;
		while (waitTime < resistTime)
		{
			waitTime += Time.deltaTime;
			if (!CanUncuff())
			{
				yield break;
			}
			else
			{
				yield return WaitFor.EndOfFrame;
			}
		}

		thisPlayerScript.playerMove.Uncuff();
		Chat.AddActionMsgToChat(thisPlayerScript.gameObject, "You have successfully removed the cuffs",
			thisPlayerScript.playerName + " has removed their cuffs");

		SoundManager.PlayNetworkedAtPos("Handcuffs", thisPlayerScript.registerTile.WorldPosition, sourceObj: gameObject);
	}

	private bool CanUncuff()
	{
		PlayerHealth playerHealth = thisPlayerScript.playerHealth;

		if (playerHealth == null ||
			playerHealth.ConsciousState == ConsciousState.DEAD ||
			playerHealth.ConsciousState == ConsciousState.UNCONSCIOUS ||
			playerHealth.OverallHealth != healthCache ||
			thisPlayerScript.registerTile.IsSlippingServer ||
			positionCache != thisPlayerScript.registerTile.LocalPositionServer)
		{
			return false;
		}

		return true;
	}

	public void CallActionClient()
	{
		PlayerManager.PlayerScript.playerNetworkActions.CmdTryUncuff();
	}
}