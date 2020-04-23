using System.Collections;
using UnityEngine;

namespace NPC
{
	public class LarvaeAI : GenericFriendlyAI, IServerSpawn
	{
		[Tooltip("Time in seconds this larva will take to become a full grown Xeno")][SerializeField]
		private float timeToGrow;

		[Tooltip("Reference to the  Xenomorph so we can spawn it")] [SerializeField]
		private GameObject xenomorph;

		protected override void Awake()
		{
			base.Awake();
			mobNameCap = char.ToUpper(mobName[0]) + mobName.Substring(1);
			BeginExploring();
		}

		protected override void DoRandomAction()
		{
			if(DMMath.Prob(5))
			{
				StartCoroutine(ChaseTail(Random.Range(1, 4)));
				StartFleeing(gameObject, 10f);
			}
			else
			{
				StartFleeing(gameObject, 10f);
			}
		}

		private IEnumerator Grow()
		{
			yield return WaitFor.Seconds(timeToGrow);
			Spawn.ServerPrefab(xenomorph, gameObject.transform.position);
			Despawn.ServerSingle(gameObject);
		}

		protected override void OnAttackReceived(GameObject damagedBy = null)
		{
			StartFleeing(damagedBy);
		}

		public override void OnDespawnServer(DespawnInfo info)
		{
			base.OnDespawnServer(info);
			StopAllCoroutines();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			StartCoroutine(Grow());
		}
	}
}