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

	private readonly Dictionary<int, ArrowSpriteVariant> directions = new Dictionary<int, ArrowSpriteVariant>()
	{
		// Cardinal arrow
		{ -90, ArrowSpriteVariant.Down },
		{ 0, ArrowSpriteVariant.Right },
		{ 90, ArrowSpriteVariant.Up },
		{ 180, ArrowSpriteVariant.Left },
		{ -180, ArrowSpriteVariant.Left }, // Wraps around

		// Diagonal arrow
		{ -135, ArrowSpriteVariant.DownLeft },
		{ -45, ArrowSpriteVariant.DownRight },
		{ 45, ArrowSpriteVariant.UpRight },
		{ 135, ArrowSpriteVariant.UpLeft }
	};

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
		if (objectToTrack != null)
		{
			Vector3 moveDirection = objectToTrack.AssumedWorldPosServer() - gameObject.AssumedWorldPosServer();
			UpdateArrowSprite(moveDirection);
		}
		else
		{
			SetArrowSpriteToNull();
		}
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
		float bestAngle = 360;
		ArrowSpriteVariant bestVariant = default;

		foreach (var kvp in directions)
		{
			var testValue = Math.Abs(kvp.Key - angle);
			if (testValue < bestAngle)
			{
				bestAngle = testValue;
				bestVariant = kvp.Value;
			}
		}

		return bestVariant;
	}

	private void ChangeArrowSprite(ArrowSprite sprite)
	{
		arrowSpriteHandler.ChangeSprite((int)sprite);
	}

	private void ChangeArrowSpriteVariant(ArrowSpriteVariant spriteVariant)
	{
		arrowSpriteHandler.ChangeSpriteVariant((int)spriteVariant);
	}

	private void SetArrowSpriteToNull()
	{
		ChangeArrowSprite(ArrowSprite.AlertNull);
		arrowSpriteHandler.ChangeSpriteVariant(0); // No variant for AlertNull.
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
		SetArrowSpriteToNull();
		arrowSpriteHandler.PushTexture();

		if (objectToTrack == null)
		{
			objectToTrack = FindObjectOfType<NukeDiskScript>().gameObject;
		}

		UpdateManager.Add(UpdateMe, scanTime);
	}

	private void ToggleOff()
	{
		arrowSpriteHandler.PushClear();
		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
	}

	#endregion Interaction
}
