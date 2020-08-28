using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ItemPinpointer : NetworkBehaviour, IInteractable<HandActivate>
{
	[SerializeField]
	private GameObject arrowGameObject = default;

	[Tooltip("How much time should lapse (seconds) between scans.")]
	[SerializeField]
	private float scanTime = 1;

	[SerializeField]
	public float maxMagnitude = 80;
	[SerializeField]
	public float mediumMagnitude = 40;
	[SerializeField]
	public float closeMagnitude = 10;

	private SpriteHandler arrowSpriteHandler;

	private GameObject objectToTrack;
	private bool isOn = false;

	private enum ArrowSprite
	{
		Null = 0,
		Alert = 1,
		AlertNull = 2,
		AlertDirect = 3,
		Direct = 4,
		Close = 5,
		Medium = 6,
		Far = 7
	}

	private enum ArrowSpriteVariant
	{
		Down = 0,
		Up = 1,
		Right = 2,
		Left = 3,
		DownRight = 4,
		DownLeft = 5,
		UpRight = 6,
		UpLeft = 7
	}

	#region Lifecycle

	private void Awake()
	{
		arrowSpriteHandler = arrowGameObject.GetComponent<SpriteHandler>();
	}

	public override void OnStartServer()
	{
		var NukeDisks = FindObjectsOfType<NukeDiskScript>();

		foreach (var nukeDisk in NukeDisks)
		{
			if (nukeDisk == null) continue;

			if (!nukeDisk.secondaryNukeDisk)
			{
				objectToTrack = nukeDisk.gameObject;
				break;
			}
		}
	}

	private void OnDisable()
	{
		ToggleOff();
	}

	#endregion Lifecycle

	private void UpdateMe()
	{
		Vector3 moveDirection = objectToTrack.AssumedWorldPosServer() - gameObject.AssumedWorldPosServer();
		UpdateArrowSprite(moveDirection);
	}

	private void UpdateArrowSprite(Vector3 moveDirection)
	{
		if (moveDirection == Vector3.zero)
		{
			ChangeArrowSprite(ArrowSprite.Direct);
			arrowSpriteHandler.ChangeSpriteVariant(0); // No variant for Direct.
			return;
		}

		ArrowSprite newSprite = GetArrowFromMagnitude(moveDirection.magnitude);
		ChangeArrowSprite(newSprite);

		float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
		ArrowSpriteVariant newVariant = GetArrowVariantFromAngle(angle);
		ChangeArrowSpriteVariant(newVariant);
	}

	private ArrowSprite GetArrowFromMagnitude(float magnitude)
	{
		if (magnitude >= mediumMagnitude) return ArrowSprite.Far;
		if (magnitude >= closeMagnitude) return ArrowSprite.Medium;
		if (magnitude <= closeMagnitude) return ArrowSprite.Close;

		return default;
	}

	private ArrowSpriteVariant GetArrowVariantFromAngle(float angle)
	{
		// Cardinal arrow
		if (angle <= -45 && angle >= -135f) return ArrowSpriteVariant.Down;
		if (angle <= 135f && angle >= 45f) return ArrowSpriteVariant.Up;
		if (angle <= 45f && angle >= -45f) return ArrowSpriteVariant.Right;
		if (angle <= 225f && angle >= 135f) return ArrowSpriteVariant.Left;

		// Diagonal arrow
		if (angle <= 0f && angle >= -90f) return ArrowSpriteVariant.DownRight;
		if (angle <= -90f && angle >= -180f) return ArrowSpriteVariant.DownLeft;
		if (angle <= 90f && angle >= 0f) return ArrowSpriteVariant.UpRight;
		if (angle <= 180f && angle >= 90f) return ArrowSpriteVariant.UpLeft;

		return default;
	}

	private void ChangeArrowSprite(ArrowSprite sprite)
	{
		arrowSpriteHandler.ChangeSprite((int)sprite);
	}

	private void ChangeArrowSpriteVariant(ArrowSpriteVariant spriteVariant)
	{
		arrowSpriteHandler.ChangeSpriteVariant((int)spriteVariant);
	}

	#region Interaction

	public void ServerPerformInteraction(HandActivate interaction)
	{
		Toggle();
	}

	private void Toggle()
	{
		isOn = !isOn;

		if (isOn)
		{
			ToggleOn();
		}
		else
		{
			ToggleOff();
		}
	}

	private void ToggleOn()
	{
		if (objectToTrack == null)
		{
			objectToTrack = FindObjectOfType<NukeDiskScript>().gameObject;
		}

		if (objectToTrack == null)
		{
			ChangeArrowSprite(ArrowSprite.AlertNull);
			arrowSpriteHandler.ChangeSpriteVariant(0); // No variant for AlertNull.
			arrowSpriteHandler.PushTexture();
		}

		UpdateMe();
		UpdateManager.Add(UpdateMe, scanTime);
	}

	private void ToggleOff()
	{
		arrowSpriteHandler.PushClear();
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	#endregion Interaction
}
