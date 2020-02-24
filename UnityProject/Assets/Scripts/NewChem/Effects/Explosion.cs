using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Mirror;


namespace Chemistry.Effects
{
	public class Explosion : ScriptableObject, IEffect
	{
		public float strength;
		[Tooltip("Explosion effect prefab, which creates when timer ends")]
		public Explosion explosionPrefab;

		public void Apply(MonoBehaviour sender, float amount)
		{
			// if (sender is NetworkBehaviour netSender)
			// {
			// 	if (netSender.isServer)
			// 	{
			// 		// Get data from grenade before despawning
			// 		var explosionMatrix = registerItem.Matrix;
			// 		var worldPos = objectBehaviour.AssumedWorldPositionServer();

			// 		// Despawn grenade
			// 		Despawn.ServerSingle(sender.gameObject);

			// 		// Explosion here
			// 		var explosionGO = Instantiate(explosionPrefab, explosionMatrix.transform);
			// 		explosionGO.transform.position = worldPos;
			// 		explosionGO.Explode(explosionMatrix);
			// 	}

			// }
		}
	}
}
