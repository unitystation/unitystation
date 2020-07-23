using System;
using UnityEngine;
using Mirror;

/// <summary>
/// Adds the rotate option to the context menu of an object. Rotates the object's directional component 90 degrees clockwise.
/// </summary>
[RequireComponent(typeof(Directional))]
public class PlayerRotatable : NetworkBehaviour, IRightClickable, ICheckedInteractable<ContextMenuApply>
{
	Directional directional;

	[SerializeField]
	[Tooltip("This will allow the object to be flipped to another object as assigned by Flipped Object.")]
	private bool isFlippable = false;

	[SerializeField]
	[Tooltip("The object to flip to when flipped.")]
	private GameObject flippedObject = null;

	private void Awake()
	{
		directional = GetComponent<Directional>();
	}

	public RightClickableResult GenerateRightClickOptions()
	{
		var result = RightClickableResult.Create();

		if (!WillInteract(ContextMenuApply.ByLocalPlayer(gameObject, null), NetworkSide.Client)) return result;

		if (isFlippable)
		{
			result.AddElement("Flip", OnFlipClicked);
		}

		return result.AddElement("Rotate", OnRotateClicked);
	}

	private void OnRotateClicked()
	{
		var menuApply = ContextMenuApply.ByLocalPlayer(gameObject, "Rotate");
		RequestInteractMessage.Send(menuApply, this);
	}

	private void OnFlipClicked()
	{
		if (!Validations.IsInReach(gameObject.RegisterTile(), PlayerManager.LocalPlayerScript.registerTile, false)) return;

		var menuApply = ContextMenuApply.ByLocalPlayer(gameObject, "Flip");
		RequestInteractMessage.Send(menuApply, this);
	}

	public bool WillInteract(ContextMenuApply interaction, NetworkSide side)
	{
		if (TryGetComponent(out ObjectBehaviour behaviour) && !behaviour.IsPushable) return false;
		if (interaction.RequestedOption == "Flip" && !isFlippable) return false;

		return DefaultWillInteract.Default(interaction, side);
	}

	public void ServerPerformInteraction(ContextMenuApply interaction)
	{
		switch (interaction.RequestedOption)
		{
			case "Rotate":
				Rotate();
				break;
			case "Flip":
				Flip();
				break;
		}
	}

	private void Rotate()
	{
		if (directional == null) return;

		// Obtains the new 90-degrees clockwise orientation of the current orientation.
		Orientation clockwise = directional.CurrentDirection.Rotate(1);
		directional.FaceDirection(clockwise);
	}

	private void Flip()
	{
		SpawnResult flippedObjectSpawn = Spawn.ServerPrefab(flippedObject, gameObject.RegisterTile().WorldPositionServer);
		if (flippedObjectSpawn.Successful)
		{
			if (flippedObjectSpawn.GameObject.TryGetComponent(out Directional directional))
			{
				var initialOrientation = directional.CurrentDirection;
				directional.FaceDirection(initialOrientation);
			}

			Despawn.ServerSingle(gameObject);
		}
		else
		{
			throw new MissingReferenceException(
					$"Failed to spawn {name}'s flipped version! Is {name}'s prefab missing reference to flippedObject prefab?");
		}
	}
}
