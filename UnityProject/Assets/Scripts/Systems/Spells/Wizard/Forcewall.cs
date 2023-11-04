using UnityEngine;
using System.Collections;
using Logs;

namespace Systems.Spells.Wizard
{
	public class Forcewall : Spell
	{
		[Tooltip("The obstruction object to spawn.")]
		[SerializeField]
		private GameObject obstructionPrefab = default;
		[Tooltip("How long these obstructions last before disappearing.")]
		[SerializeField, Range(1, 600)]
		private int lifespan = 50;
		[Tooltip("Set the trait used to allow the caster to pass through the obstruction. Must be the same as defined on the object.")]
		[SerializeField]
		private PassableExclusionTrait exclusionTrait = default;

		public override void CallActionClient()
		{
			SetCasterPassable(PlayerManager.LocalPlayerObject);
			base.CallActionClient();
		}

		public override bool CastSpellServer(PlayerInfo caster)
		{
			GameObject[] obstructions = new GameObject[3];
			obstructions[0] = Spawn.ServerPrefab(obstructionPrefab, caster.Script.WorldPos).GameObject;

			if (caster.GameObject.TryGetComponent<Rotatable>(out var directional))
			{
				if (directional.CurrentDirection == OrientationEnum.Down_By180 || directional.CurrentDirection == OrientationEnum.Up_By0)
				{
					obstructions[1] = Spawn.ServerPrefab(obstructionPrefab, caster.Script.WorldPos + Vector3.left).GameObject;
					obstructions[2] = Spawn.ServerPrefab(obstructionPrefab, caster.Script.WorldPos + Vector3.right).GameObject;
				}
				else if (directional.CurrentDirection == OrientationEnum.Left_By90 || directional.CurrentDirection == OrientationEnum.Right_By270)
				{
					obstructions[1] = Spawn.ServerPrefab(obstructionPrefab, caster.Script.WorldPos + Vector3.up).GameObject;
					obstructions[2] = Spawn.ServerPrefab(obstructionPrefab, caster.Script.WorldPos + Vector3.down).GameObject;
				}
			}

			SetCasterPassable(caster.GameObject);
			StartCoroutine(DespawnObstructions(obstructions));

			return true;
		}

		private void SetCasterPassable(GameObject caster)
		{
			if (caster.TryGetComponent<PassableExclusionHolder>(out var holder))
			{
				holder.passableExclusions.Add(exclusionTrait);
			}
			else
			{
				Loggy.LogError($"{nameof(PassableExclusionHolder)} not found on {caster}!", Category.Spells);
			}
		}

		private IEnumerator DespawnObstructions(GameObject[] obstructions)
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
