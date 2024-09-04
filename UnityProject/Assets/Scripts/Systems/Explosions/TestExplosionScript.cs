using System.Collections;
using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Systems.Explosions
{
	public class TestExplosionScript : MonoBehaviour
	{
		private UniversalObjectPhysics objectBehaviour;
		private RegisterObject registerObject;

		private void Awake()
		{
			objectBehaviour = this.GetComponent<UniversalObjectPhysics>();
			registerObject = this.GetComponent<RegisterObject>();
		}

		public float Strength = 9000;

		[RightClickMethod, NaughtyAttributes.Button]
		void StartExplosion()
		{
			if (registerObject == null)
			{
				Systems.Explosions.Explosion.StartExplosion(objectBehaviour.registerTile.WorldPositionServer, Strength, stunNearbyPlayers : true);
			}
			else
			{
				Explosion.StartExplosion(registerObject.WorldPositionServer, Strength);
			}
			//Loggy.Log("RequestPulse!!" + Time.time);
		}
	}
}
