using System;
using UnityEngine;
using Mirror;

/// <summary>
/// Adds the rotate option to the context menu of an object. Rotates the object's directional component 90 degrees clockwise.
/// </summary>
public class PlayerRotatable : NetworkBehaviour, IRightClickable, ICheckedInteractable<ContextMenuApply>, ICheckedInteractable<HandApply>
{
	private Directional directional;

	[SyncVar(hook = nameof(SyncRotation))]
	private float zRotation = 0;

	private void Awake()
	{
		directional = GetComponent<Directional>();
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (TryGetComponent(out ObjectBehaviour behaviour) && !behaviour.IsPushable) return false;

		return interaction.IsAltClick;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		Rotate();
	}

	public RightClickableResult GenerateRightClickOptions()
	{
		var result = RightClickableResult.Create();

		if (!WillInteract(ContextMenuApply.ByLocalPlayer(gameObject, null), NetworkSide.Client)) return result;

		return result.AddElement("Rotate", OnRotateClicked);
	}

	public bool WillInteract(ContextMenuApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (TryGetComponent(out ObjectBehaviour behaviour) && !behaviour.IsPushable) return false;

		return true;
	}

	public void ServerPerformInteraction(ContextMenuApply interaction)
	{
		Rotate();
	}

	private void OnRotateClicked()
	{
		var menuApply = ContextMenuApply.ByLocalPlayer(gameObject, "Rotate");
		RequestInteractMessage.Send(menuApply, this);
	}

	public void Rotate()
	{
		if (directional != null)
		{
			// Obtains the new 90-degrees clockwise orientation of the current orientation.
			Orientation clockwise = directional.CurrentDirection.Rotate(1);
			directional.FaceDirection(clockwise);
		}
		else
		{
			Debug.Log($"{this} is rotating transform...");
			transform.Rotate(0, 0, -90);
			SyncRotation(zRotation, transform.eulerAngles.z);
		}
	}

	public void SyncRotation(float oldZ, float newZ)
	{
		zRotation = newZ;
		transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, newZ);
	}
}
