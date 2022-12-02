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
		[SerializeField, Range(1, MAX_LIFESPAN)]
		private int lifespan = 50;

		private const int MAX_LIFESPAN = 600;

		private List<GameObject> allObstructions = new List<GameObject>();


		public override void DoEffectAura(GameObject centeredAround)
		{
			var objCenter = centeredAround.AssumedWorldPosServer().RoundToInt();

			var positions = EffectShape.CreateEffectShape(effectShapeType, objCenter, AuraRadius);
			List<GameObject> obstructions = new List<GameObject>();

			foreach(Vector3Int position in positions)
			{
				GameObject obstruction = Spawn.ServerPrefab(obstructionPrefab, position).GameObject;
				obstructions.Add(obstruction);
				allObstructions.Add(obstruction);
			}

			if(centeredAround.TryGetComponent<Artifact>(out var parentArtifact) == false) return;

			parentArtifact.StartCoroutine(this.DespawnObstructions(obstructions));
			
		}

		public IEnumerator DespawnObstructions(List<GameObject> obstructions)
		{
			yield return new WaitForSeconds(lifespan);
			
			foreach (GameObject obstruction in obstructions)
			{
				if (obstruction == null) continue;
				allObstructions.Remove(obstruction);

				_ = Despawn.ServerSingle(obstruction);
			}
		}

		public void TerminateObstructions()
		{
			List<GameObject> obstructionsToRemove = new List<GameObject>(allObstructions);

			foreach (GameObject obstruction in obstructionsToRemove)
			{
				if (obstruction == null) continue;
				allObstructions.Remove(obstruction);

				_ = Despawn.ServerSingle(obstruction);
			}

			allObstructions.Clear();
		}
	}
}
