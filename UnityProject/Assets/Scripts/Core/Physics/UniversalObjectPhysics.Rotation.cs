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

	}
}