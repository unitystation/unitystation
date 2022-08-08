using UnityEngine;


namespace Systems.Research
{
	[CreateAssetMenu(fileName = "SpawningArtifactEffect", menuName = "ScriptableObjects/Systems/Artifacts/SpawnPrefabAreaEffect")]
	public class SpawningArtifactEffect : AreaEffectBase
	{
		public int amountToSummon = 1;
		public GameObject objectToSpawn = null;

		public bool avoidSpace = true;
		public bool avoidImpassable = true;

		public override void DoEffectAura(GameObject centeredAround)
		{
			var objCenter = centeredAround.AssumedWorldPosServer().RoundToInt();
			var shape = EffectShape.CreateEffectShape(effectShapeType, objCenter, AuraRadius);
			Matrix matrix = centeredAround.GetComponent<RegisterObject>().Matrix;

			for (int i = 0; i < amountToSummon; i++)
			{
				var pos = shape.PickRandom();
				if(avoidSpace)
				{
					if (matrix.IsSpaceAt(pos, true)) continue; //Could put this in one if, but dont wanna do matrix checks unless we really have to
				}
				if(avoidImpassable)
				{
					if(matrix.IsWallAt(pos, true)) continue;
				}

				Spawn.ServerPrefab(objectToSpawn, SpawnDestination.At(shape.PickRandom()));
			}
		}
	}
}