using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Electric.Inheritance;
using UnityEngine;
using Mirror;

/// <summary>
/// Allows object to function as a door switch - opening / closing door when clicked.
/// </summary>
public class DoorSwitch : SubscriptionController, ICheckedInteractable<HandApply>, ISetMultitoolMaster
{
	private SpriteRenderer spriteRenderer;
	public Sprite greenSprite;
	public Sprite offSprite;
	public Sprite redSprite;

	[Header("Access Restrictions for ID")] [Tooltip("Is this door restricted?")]
	public bool restricted;

	[Tooltip("Access level to limit door if above is set.")]
	public Access access;


	public List<DoorController> doorControllers = new List<DoorController>();

	private bool buttonCoolDown = false;
	private AccessRestrictions accessRestrictions;

	[SerializeField]
	private MultitoolConnectionType conType = MultitoolConnectionType.DoorButton;
	public MultitoolConnectionType ConType  => conType;

	private bool multiMaster = true;
	public bool MultiMaster => multiMaster;

	public void AddSlave(object SlaveObject)
	{
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		if (doorControllers.Count > 0)
		{
			foreach (var door in doorControllers)
			{
				if (door.IsHackable)
				{
					HackingNode outsideSignalOpen = door.HackingProcess.GetNodeWithInternalIdentifier(HackingIdentifier.OutsideSignalOpen);
					outsideSignalOpen.AddConnectedNode(door.HackingProcess.GetNodeWithInternalIdentifier(HackingIdentifier.OpenDoor));

					HackingNode outsideSignalClose = door.HackingProcess.GetNodeWithInternalIdentifier(HackingIdentifier.OutsideSignalClose);
					outsideSignalClose.AddConnectedNode(door.HackingProcess.GetNodeWithInternalIdentifier(HackingIdentifier.CloseDoor));
				}
			}
		}

	}


	private void Start()
	{
		//This is needed because you can no longer apply shutterSwitch prefabs (it will move all of the child sprite positions)
		gameObject.layer = LayerMask.NameToLayer("WallMounts");
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		accessRestrictions = gameObject.AddComponent<AccessRestrictions>();
		if (restricted)
		{
			accessRestrictions.restriction = access;
		}
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		//this validation is only done client side for their convenience - they can't
		//press button while it's animating.
		if (side == NetworkSide.Client)
		{
			if (buttonCoolDown) return false;
			buttonCoolDown = true;
			StartCoroutine(CoolDown());
		}

		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (accessRestrictions != null && restricted)
		{
			if (!accessRestrictions.CheckAccess(interaction.Performer))
			{
				RpcPlayButtonAnim(false);
				return;
			}
		}

		RunDoorController();
		RpcPlayButtonAnim(true);

	}

	private void RunDoorController()
	{
		foreach (DoorController door in doorControllers)
		{
			if (door.IsClosed)
			{
				if (door.IsHackable)
				{
					door.HackingProcess.SendOutputToConnectedNodes(HackingIdentifier.OutsideSignalOpen);
				}
				else
				{
					door.TryOpen();
				}
			}
			else
			{
				if (door.IsHackable)
				{
					door.HackingProcess.SendOutputToConnectedNodes(HackingIdentifier.OutsideSignalClose);
				}
				else
				{
					door.TryClose();
				}
			}
		}
	}

	//Stops spamming from players
	IEnumerator CoolDown()
	{
		yield return WaitFor.Seconds(1.2f);
		buttonCoolDown = false;
	}

	[ClientRpc]
	public void RpcPlayButtonAnim(bool status)
	{
		StartCoroutine(ButtonFlashAnim(status));
	}

	IEnumerator ButtonFlashAnim(bool status)
	{
		if (spriteRenderer == null)
		{
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		}

		for (int i = 0; i < 6; i++)
		{
			if (status)
			{
				if (spriteRenderer.sprite == greenSprite)
				{
					spriteRenderer.sprite = offSprite;
				}
				else
				{
					spriteRenderer.sprite = greenSprite;
				}

				yield return WaitFor.Seconds(0.2f);
			}
			else
			{
				if (spriteRenderer.sprite == redSprite)
				{
					spriteRenderer.sprite = offSprite;
				}
				else
				{
					spriteRenderer.sprite = redSprite;
				}

				yield return WaitFor.Seconds(0.1f);
			}
		}

		spriteRenderer.sprite = greenSprite;
	}

	#region Editor

	void OnDrawGizmosSelected()
	{
		var sprite = GetComponentInChildren<SpriteRenderer>();
		if (sprite == null)
			return;

		//Highlighting all controlled doors with red lines and spheres
		Gizmos.color = new Color(1, 0.5f, 0, 1);
		for (int i = 0; i < doorControllers.Count; i++)
		{
			var doorController = doorControllers[i];
			if(doorController == null) continue;
			Gizmos.DrawLine(sprite.transform.position, doorController.transform.position);
			Gizmos.DrawSphere(doorController.transform.position, 0.25f);
		}
	}

	public override IEnumerable<GameObject> SubscribeToController(IEnumerable<GameObject> potentialObjects)
	{
		var approvedObjects = new List<GameObject>();

		foreach (var potentialObject in potentialObjects)
		{
			var doorController = potentialObject.GetComponent<DoorController>();
			if (doorController == null) continue;
			AddDoorControllerFromScene(doorController);
			approvedObjects.Add(potentialObject);
		}

		return approvedObjects;
	}

	private void AddDoorControllerFromScene(DoorController doorController)
	{
		if (doorControllers.Contains(doorController))
		{
			doorControllers.Remove(doorController);
		}
		else
		{
			doorControllers.Add(doorController);
		}
	}

	#endregion

}