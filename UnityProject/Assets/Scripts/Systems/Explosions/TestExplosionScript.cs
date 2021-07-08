using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.Explosions
{
	public class TestExplosionScript : MonoBehaviour
	{
		private ObjectBehaviour objectBehaviour;
		private RegisterObject registerObject;

		private void Awake()
		{
			objectBehaviour = this.GetComponent<ObjectBehaviour>();
			registerObject = this.GetComponent<RegisterObject>();
		}

		public float Strength = 9000;

		[RightClickMethod]
		void StartExplosion()
		{
			if (registerObject == null)
			{
				Systems.Explosions.Explosion.StartExplosion(objectBehaviour.registerTile.LocalPosition, Strength,
					objectBehaviour.registerTile.Matrix);
			}
			else
			{
				Explosion.StartExplosion(registerObject.LocalPosition, Strength,
					registerObject.Matrix);
			}
			//Logger.Log("RequestPulse!!" + Time.time);
		}
	}
}
