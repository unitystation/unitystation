using UnityEngine;
using US13.Tilemaps.Behaviours.Objects;
using US13.UI.Core.RightClick;
using UniversalObjectPhysics = US13.Core.Physics.UniversalObjectPhysics;

namespace US13.Systems.Explosions
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
