using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConveyorBeltSwitch : MonoBehaviour, ICheckedInteractable<HandApply>
{
	public SpriteRenderer spriteRenderer;

	public Sprite Forward;
	public Sprite Backward;
	public Sprite Off;

	private Sprite LastSprite;

	public int CurrentState = 1; // 0 Backwards, 1 Off, 2 Forwards

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		if (!Validations.IsTarget(gameObject, interaction)) return false;

		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (spriteRenderer.sprite == Forward)
		{
			LastSprite = spriteRenderer.sprite;

			spriteRenderer.sprite = Off;
			CurrentState = 1;
		}
		else if (spriteRenderer.sprite == Backward)
		{
			LastSprite = spriteRenderer.sprite;

			spriteRenderer.sprite = Off;
			CurrentState = 1;
		}
		else if(LastSprite == Forward)
		{
			spriteRenderer.sprite = Backward;
			CurrentState = 0;
		}
		else
		{
			spriteRenderer.sprite = Forward;
			CurrentState = 2;
		}
	}
}
