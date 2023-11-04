using System;
using Logs;
using Messages.Client.Interaction;
using UnityEngine;

namespace Objects
{
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
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (TryGetComponent(out UniversalObjectPhysics behaviour) && behaviour.IsNotPushable) return false;

			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(ContextMenuApply interaction)
		{
			Flip();
		}

		private void OnFlipClicked()
		{
			if (!Validations.IsReachableByRegisterTiles(gameObject.RegisterTile(), PlayerManager.LocalPlayerScript.RegisterPlayer, false)) return;

			var menuApply = ContextMenuApply.ByLocalPlayer(gameObject, "Flip");
			RequestInteractMessage.Send(menuApply, this);
		}

		private void Flip()
		{
			SpawnResult flippedObjectSpawn = Spawn.ServerPrefab(flippedObject, gameObject.RegisterTile().WorldPositionServer);
			if (flippedObjectSpawn.Successful)
			{
				if (flippedObjectSpawn.GameObject.TryGetComponent(out Rotatable directional))
				{
					var initialOrientation = directional.CurrentDirection;
					directional.FaceDirection(initialOrientation);
				}

				_ = Despawn.ServerSingle(gameObject);
			}
			else
			{
				Loggy.LogError(
						$"Failed to spawn {name}'s flipped version! " +
						$"Is {name} missing reference to {nameof(flippedObject)} prefab?", Category.Interaction);
			}
		}
	}
}
