using System;
using System.Collections;
using UnityEngine;
using WebSocketSharp;
using Random = UnityEngine.Random;

namespace NPC
{
	public class LarvaeAI : GenericFriendlyAI
	{
		[Tooltip("Time in seconds this larva will take to become a full grown Xeno")][SerializeField]
		private float timeToGrow = 200;

		[Tooltip("Reference to the  Xenomorph so we can spawn it")] [SerializeField]
		private GameObject xenomorph = null;

		protected override void Awake()
		{
			base.Awake();
			mobNameCap = mobName.IsNullOrEmpty() ? mobName : char.ToUpper(mobName[0]) + mobName.Substring(1);
			simpleAnimal = GetComponent<SimpleAnimal>();
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
			if (IsDead || IsUnconscious)
			{
				yield break;
			}

			Spawn.ServerPrefab(xenomorph, gameObject.transform.position);
			Despawn.ServerSingle(gameObject);
		}

		protected override void OnAttackReceived(GameObject damagedBy = null)
		{
			StartFleeing(damagedBy);
		}

		protected override void OnSpawnMob()
		{
			base.OnSpawnMob();
			StartFleeing(gameObject, 10f);
			StartCoroutine(Grow());
		}

		public override void OnDespawnServer(DespawnInfo info)
		{
			base.OnDespawnServer(info);
			StopAllCoroutines();
		}
	}
}