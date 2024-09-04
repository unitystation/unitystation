using UnityEngine;

namespace Core
{
	public partial class UniversalObjectPhysics : IRightClickable
	{
		[SerializeField] private bool canResetRotationFromRightClick = false;

		public virtual RightClickableResult GenerateRightClickOptions()
		{
			var options = RightClickableResult.Create();

			if (string.IsNullOrEmpty(PlayerList.Instance.AdminToken) == false &&
			    KeyboardInputManager.Instance.CheckKeyAction(KeyAction.ShowAdminOptions,
				    KeyboardInputManager.KeyEventType.Hold))
			{
				options.AddAdminElement("Teleport To", AdminTeleport)
					.AddAdminElement("Toggle Pushable", AdminTogglePushable);
			}

			//check if our local player can reach this
			var initiator = PlayerManager.LocalMindScript.GetDeepestBody().GetComponent<UniversalObjectPhysics>();
			if (initiator == null) return options;

			//if it's pulled by us
			if (PulledBy.HasComponent && PulledBy.Component == initiator)
			{
				//already pulled by us, but we can stop pulling
				options.AddElement("StopPull", ClientTryTogglePull);
			}
			else
			{
				// Check if in range for pulling, not trying to pull itself and it can be pulled.
				if (Validations.IsReachableByRegisterTiles(initiator.registerTile, registerTile, false,
					    context: gameObject) &&
				    isNotPushable == false && initiator != this)
				{
					options.AddElement("Pull", ClientTryTogglePull);
				}
			}

			if (canResetRotationFromRightClick && rotationTarget != null)
			{
				if (Validations.IsReachableByRegisterTiles(initiator.registerTile, registerTile, false,
					    context: gameObject) &&
				    rotationTarget.eulerAngles != initialRotationOnAwake)
				{
					options.AddElement("Reset Rotation", CmdResetTransformRotationForAll);
				}
			}

			return options;
		}
	}
}