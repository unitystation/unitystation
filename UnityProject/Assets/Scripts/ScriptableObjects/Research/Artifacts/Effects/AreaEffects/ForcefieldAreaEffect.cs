using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Objects.Research;

namespace Systems.Research
{
	[CreateAssetMenu(fileName = "ForcefieldAreaEffect", menuName = "ScriptableObjects/Systems/Artifacts/ForcefieldAreaEffect")]
	public class ForcefieldAreaEffect : AreaEffectBase
	{
		[Tooltip("The obstruction object to spawn.")]
		[SerializeField]
		private GameObject obstructionPrefab = default;
		[Tooltip("How long these obstructions last before disappearing.")]
		[SerializeField, Range(1, 600)]
		private int lifespan = 50;

		public override void DoEffectAura(GameObject centeredAround)
		{
			var objCenter = centeredAround.AssumedWorldPosServer().RoundToInt();

			var positions = EffectShape.CreateEffectShape(effectShapeType, objCenter, AuraRadius);
			List<GameObject> obstructions = new List<GameObject>();

			foreach(Vector3Int position in positions)
			{
				GameObject obstruction = Spawn.ServerPrefab(obstructionPrefab, position).GameObject;
				obstructions.Add(obstruction);
			}

			centeredAround.TryGetComponent<Artifact>(out var parentArtifact);

			if (parentArtifact == null) return;

			parentArtifact.StartCoroutine(this.DespawnObstructions(obstructions));
			
		}

		public IEnumerator DespawnObstructions(List<GameObject> obstructions)
		{
			yield return WaitFor.Seconds(lifespan);

			foreach (GameObject obstruction in obstructions)
			{
				if (obstruction == null) continue;

				_ = Despawn.ServerSingle(obstruction);
			}
		}
	}
}
