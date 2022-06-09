using Mirror;
using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	public class BulletCasing : NetworkBehaviour
	{
		public GameObject spriteObj;

		private void Start()
		{
			if (isServer)
			{
				var netTransform = GetComponent<UniversalObjectPhysics>();

				if(netTransform != null)
				{
					netTransform.ForceDrop(netTransform.transform.position + new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(-0.6f, 0.6f)));
				}
			}

			var axis = new Vector3(0, 0, 1);
			spriteObj.transform.localRotation = Quaternion.AngleAxis(Random.Range(-180f, 180f), axis);
		}
	}
}
