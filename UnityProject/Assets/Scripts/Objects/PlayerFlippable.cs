using System;
using UnityEngine;

/// <summary>
/// Adds the flip option to the context menu of an object. Replaces the object with the prefab defined in inspector.
/// </summary>
public class PlayerFlippable : MonoBehaviour, IRightClickable, ICheckedInteractable<ContextMenuApply>
{
	[SerializeField]
	[Tooltip("The object to flip to when flipped.")]
	private GameObject flippedObject = default;

	public RightClickableResult GenerateRightClickOptions()
	{
		var result = RightClickableResult.Create();

		if (!WillInteract(ContextMenuApply.ByLocalPlayer(gameObject, null), NetworkSide.Client)) return result;

		return result.AddElement("Flip", OnFlipClicked);
	}

	public bool WillInteract(ContextMenuApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (TryGetComponent(out ObjectBehaviour behaviour) && !behaviour.IsPushable) return false;

		return DefaultWillInteract.Default(interaction, side);
	}

	public void ServerPerformInteraction(ContextMenuApply interaction)
	{
		Flip();
	}

	private void OnFlipClicked()
	{
		if (!Validations.IsInReach(gameObject.RegisterTile(), PlayerManager.LocalPlayerScript.registerTile, false)) return;

		var menuApply = ContextMenuApply.ByLocalPlayer(gameObject, "Flip");
		RequestInteractMessage.Send(menuApply, this);
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
			Logger.LogError(
					$"Failed to spawn {name}'s flipped version! " +
					$"Is {name} missing reference to {nameof(flippedObject)} prefab?");
		}
	}
}
