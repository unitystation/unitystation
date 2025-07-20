using Mirror;
using UnityEngine;

namespace Core.Physics
{
	public partial class UniversalObjectPhysics
	{
		protected Rotatable rotatable;
		[SerializeField] protected Transform rotationTarget;


		private void SetRotationTarget()
		{
			rotationTarget = transform;
		}

		private void SetRotationTargetWhenNull()
		{

			rotationTarget = transform;
		}

		[Command(requiresAuthority = false)]
		protected void CmdResetTransformRotationForAll(NetworkConnectionToClient sender = null)
		{
			if (sender == null) return;
			if (Validations.CanApply(PlayerList.Instance.Get(sender).Script, this.gameObject, NetworkSide.Server, false, ReachRange.Standard) == false) return;

			var Euler = transform.localRotation.eulerAngles;
			Euler.z = 0;
			transform.localRotation = Quaternion.Euler(Euler);
		}

	}
}