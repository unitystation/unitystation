using System;
using Logs;
using Systems.Spawns;
using UnityEngine;

namespace Core.Characters.AttributeBehaviors
{
	/// <summary>
	/// Character attribute behavior that spawns a character at a job type destination.
	/// </summary>
	public class SpawnAtJobType : CharacterAttributeBehavior
	{
		[SerializeField] private JobType jobType = JobType.ASSISTANT;
		private static readonly DateTime ARRIVALS_SPAWN_TIME = new DateTime().AddHours(12).AddMinutes(2);

		public override void Run(GameObject characterBody)
		{
			Transform spawnTransform;
			var physics = characterBody.GetComponent<UniversalObjectPhysics>();
			// Spawn normal location for special jobs or if less than 2 minutes passed
			if (GameManager.Instance.RoundTime < ARRIVALS_SPAWN_TIME)
			{
				spawnTransform = SpawnPoint.GetRandomPointForJob(jobType);
			}
			else
			{
				spawnTransform = SpawnPoint.GetRandomPointForLateSpawn();
				// Fallback to assistant spawn location if none found for late join
				if (spawnTransform == null && jobType != JobType.NULL)
				{
					spawnTransform = SpawnPoint.GetRandomPointForJob(JobType.ASSISTANT);
				}
			}

			if (spawnTransform == null)
			{
				Loggy.LogErrorFormat(
					"Unable to determine spawn position for  occupation {0}. Cannot spawn player.",
					Category.EntitySpawn, jobType);
				return;
			}

			// so we don't conver this twice
			var position = spawnTransform.localPosition.CutToInt();
			// getting matrix based of global position in the game world
			var matrixInfo = MatrixManager.AtPoint(position, true);
			// Setting the player at the correct position and matrix.
			physics.ForceSetLocalPosition(position.ToLocal(matrixInfo.Matrix),
				Vector2.zero, false, matrixInfo.Id, true, 0);
		}

	}
}