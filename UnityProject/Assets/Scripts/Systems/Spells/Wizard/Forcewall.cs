using UnityEngine;
using System.Collections;
using Mirror;

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
			SetCasterPassable(PlayerManager.LocalPlayer);
			base.CallActionClient();
		}

		public override bool CastSpellServer(ConnectedPlayer caster)
		{
			GameObject[] obstructions = new GameObject[3];
			obstructions[0] = Spawn.ServerPrefab(obstructionPrefab, caster.Script.WorldPos).GameObject;

			if (caster.GameObject.TryGetComponent<Directional>(out var directional))
			{
				if (directional.CurrentDirection == Orientation.Down || directional.CurrentDirection == Orientation.Up)
				{
					obstructions[1] = Spawn.ServerPrefab(obstructionPrefab, caster.Script.WorldPos + Vector3.left).GameObject;
					obstructions[2] = Spawn.ServerPrefab(obstructionPrefab, caster.Script.WorldPos + Vector3.right).GameObject;
				}
				else if (directional.CurrentDirection == Orientation.Left || directional.CurrentDirection == Orientation.Right)
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
				Logger.LogError($"{nameof(PassableExclusionHolder)} not found on {caster}!", Category.Spells);
			}
		}

		private IEnumerator DespawnObstructions(GameObject[] obstructions)
		{
			yield return WaitFor.Seconds(lifespan);

			foreach (GameObject obstruction in obstructions)
			{
				if (obstruction == null) continue;

				Despawn.ServerSingle(obstruction);
			}
		}
	}
}
