using System;
using Messages.Client.Interaction;
using UnityEngine;
using Mirror;

namespace Objects
{
	/// <summary>
	/// Adds the rotate option to the context menu of an object. Rotates the object's directional component 90 degrees clockwise.
	/// </summary>
	public class PlayerRotatable : NetworkBehaviour, IRightClickable, ICheckedInteractable<ContextMenuApply>, ICheckedInteractable<HandApply>
	{
		private Rotatable rotatable;

		[SerializeField] [Tooltip("Allows to be rotated when It cannot be moved")]
		private bool CanRotateIfNotMovable = false;

		private void Awake()
		{
			rotatable = GetComponent<Rotatable>();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (TryGetComponent(out UniversalObjectPhysics behaviour) && behaviour.IsNotPushable) return false;

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
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (TryGetComponent(out UniversalObjectPhysics behaviour) && (behaviour.IsNotPushable && CanRotateIfNotMovable == false)) return false;

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
			if (rotatable != null)
			{
				// Obtains the new 90-degrees clockwise orientation of the current orientation.
				rotatable.RotateBy(1);
			}

		}
	}
}
