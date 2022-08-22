using UnityEngine;


namespace Systems.Research
{
	[CreateAssetMenu(fileName = "SpawningArtifactEffect", menuName = "ScriptableObjects/Systems/Artifacts/SpawnPrefabAreaEffect")]
	public class SpawningArtifactEffect : AreaEffectBase
	{
		[SerializeField]
		public int amountToSummon = 1;
		[SerializeField]
		private GameObject objectToSpawn = null;

		[SerializeField]
		private bool avoidSpace = true;
		[SerializeField]
		private bool avoidImpassable = true;

		public override void DoEffectAura(GameObject centeredAround)
		{
			var objCenter = centeredAround.AssumedWorldPosServer().RoundToInt();
			var shape = EffectShape.CreateEffectShape(effectShapeType, objCenter, AuraRadius);
			Matrix matrix = centeredAround.GetComponent<RegisterObject>().Matrix;

			for (int i = 0; i < amountToSummon; i++)
			{
				var pos = shape.PickRandom();

				if(avoidSpace && matrix.IsSpaceAt(pos, true)) continue;

				if(avoidImpassable && (matrix.IsWallAt(pos, true))) continue;
				
				Spawn.ServerPrefab(objectToSpawn, SpawnDestination.At(shape.PickRandom()));
			}
		}
	}
}