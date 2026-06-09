using Logs;
using UnityEngine;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Messages.Client.Interaction;
using US13.Objects.Directionals;
using US13.Player;
using US13.UI.Core.RightClick;
using Util;
using UniversalObjectPhysics = US13.Core.Physics.UniversalObjectPhysics;

namespace US13.Objects
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
				Loggy.Error(
						$"Failed to spawn {name}'s flipped version! " +
						$"Is {name} missing reference to {nameof(flippedObject)} prefab?", Category.Interaction);
			}
		}
	}
}
