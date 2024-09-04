using Mirror;
using UnityEngine;

namespace Core.Physics
{
	public partial class UniversalObjectPhysics
	{
		protected Rotatable rotatable;
		private Vector3 initialRotationOnAwake = Vector3.zero;
		[SerializeField] protected Transform rotationTarget;

		private void SetInitialRotationTarget()
		{
			if (rotationTarget != null) return;
			initialRotationOnAwake = rotationTarget.rotation.eulerAngles;
		}

		private void SetRotationTarget()
		{
			if (rotationTarget != null) return;
			if (this is MovementSynchronisation c && c.playerScript.RegisterPlayer.LayDownBehavior != null)
			{
				rotationTarget = c.playerScript.RegisterPlayer.LayDownBehavior.Sprites;
				SetRotationTargetWhenNull();
				return;
			}
			rotationTarget = transform;
			SetInitialRotationTarget();
		}

		private void SetRotationTargetWhenNull()
		{
			if (rotationTarget != null) return;
			rotationTarget = transform;
		}

		[Command(requiresAuthority = false)]
		protected void CmdResetTransformRotationForAll()
		{
			ResetTransformRotationForAll();
		}

		[ClientRpc]
		protected void ResetTransformRotationForAll()
		{
			if (rotationTarget == null) return;
			rotationTarget.rotation = Quaternion.Euler(initialRotationOnAwake);
		}
	}
}