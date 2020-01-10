using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// Handles the overlays for the handcuff sprites
/// </summary>
public class RestraintOverlay : ClothingItem
{
	//TODO Different colored overlays for different restraints
	[SerializeField]
	private List<Sprite> handCuffOverlays = new List<Sprite>();

	[SerializeField] private SpriteRenderer spriteRend;
	private CancellationTokenSource cancelSource;

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

	void DetermineAlertUI()
	{
		if (thisPlayerScript != PlayerManager.PlayerScript) return;

		if (GameObjectReference != null)
		{
			UIManager.AlertUI.ToggleAlertCuffed(true);
		}
		else
		{
			UIManager.AlertUI.ToggleAlertCuffed(false);
		}
	}

	public void ServerBeginUnCuffAttempt()
	{
		if (cancelSource != null)
		{
			cancelSource.Cancel();
		}
		cancelSource = new CancellationTokenSource();
		StartCoroutine(UncuffCountDown(cancelSource.Token));
		Chat.AddActionMsgToChat(thisPlayerScript.gameObject, "You are attempting to remove the cuffs. This takes up to 30 seconds",
			thisPlayerScript.playerName + " is attempting to remove their cuffs");
	}

	IEnumerator UncuffCountDown(CancellationToken cancelToken)
	{
		float waitTime = 0f;
		bool canUncuff = false;
		while (!canUncuff && !cancelToken.IsCancellationRequested)
		{
			waitTime += Time.deltaTime;

			if (waitTime > 30f)
			{
				canUncuff = true;
				thisPlayerScript.playerMove.Uncuff();
				Chat.AddActionMsgToChat(thisPlayerScript.gameObject, "You have successfully removed the cuffs",
					thisPlayerScript.playerName + " has removed their cuffs");

				SoundManager.PlayNetworkedAtPos("Handcuffs", thisPlayerScript.registerTile.WorldPosition);
			}
			yield return WaitFor.EndOfFrame;
		}
	}
}
