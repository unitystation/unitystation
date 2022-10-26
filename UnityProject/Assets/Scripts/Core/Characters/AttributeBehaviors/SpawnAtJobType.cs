
using System;
using Systems.Spawns;
using UnityEngine;

namespace Core.Characters.AttributeBehaviors
{
	public abstract class SpawnAtJobType : CharacterAttributeBehavior
	{
		[SerializeField] private JobType jobType = JobType.ASSISTANT;
		private static readonly DateTime ARRIVALS_SPAWN_TIME = new DateTime().AddHours(12).AddMinutes(2);

		public override void Run(GameObject characterBody)
		{
			Transform spawnTransform;
			//Spawn normal location for special jobs or if less than 2 minutes passed
			if (GameManager.Instance.stationTime < ARRIVALS_SPAWN_TIME)
			{
				spawnTransform = SpawnPoint.GetRandomPointForJob(jobType);
			}
			else
			{
				spawnTransform = SpawnPoint.GetRandomPointForLateSpawn();
				//Fallback to assistant spawn location if none found for late join
				if (spawnTransform == null && jobType != JobType.NULL)
				{
					spawnTransform = SpawnPoint.GetRandomPointForJob(JobType.ASSISTANT);
				}
			}

			if (spawnTransform == null)
			{
				Logger.LogErrorFormat(
					"Unable to determine spawn position for  occupation {0}. Cannot spawn player.",
					Category.EntitySpawn, jobType);
				return;
			}
		}
	}
}