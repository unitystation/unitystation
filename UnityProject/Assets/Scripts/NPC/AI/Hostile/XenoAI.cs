using System.Collections;
using HealthV2;
using NaughtyAttributes;
using UnityEngine;

namespace Systems.MobAIs
{
	public class XenoAI: GenericHostileAI
	{
		[SerializeField]
		[Tooltip("If true, this Xeno won't ever attempt to become Queen.")]
		private bool disableAscension = false;

		[SerializeField]
		[HideIf(nameof(disableAscension))]
		[Tooltip("How many queens could there be at once in a round. " +
		         "If this cap is not reached, this Xeno could become a Queen to preserve the perfect life form!")]
		private int queenCap = 1;

		[SerializeField]
		[HideIf(nameof(disableAscension))]
		[Tooltip("Reference to the Queen prefab to be spawned if this xeno becomes a Queen")]
		private GameObject queenPrefab = default;

		protected override void OnAIStart()
		{
			base.OnAIStart();
			TryBecomingQueen();
		}

		/// <summary>
		/// Looks around and tries to find players to target
		/// </summary>
		/// <returns>Gameobject of the first player it found</returns>
		protected override GameObject SearchForTarget()
		{
			var player = Physics2D.OverlapCircleAll(registerObject.WorldPositionServer.To2Int(), 20f, hitMask);
			//var hits = coneOfSight.GetObjectsInSight(hitMask, LayerTypeSelection.Walls, dirSprites.CurrentFacingDirection, 10f, 20);
			if (player.Length == 0)
			{
				return null;
			}

			foreach (var coll in player)
			{
				if (MatrixManager.Linecast(
					    gameObject.AssumedWorldPosServer(),
					    LayerTypeSelection.Walls,
					    null,
					    coll.gameObject.AssumedWorldPosServer()).ItHit == false)
				{
					if(coll.gameObject.TryGetComponent<LivingHealthMasterBase>(out var healthMasterBase) == false || healthMasterBase.IsDead) continue;

					if(healthMasterBase.playerScript.PlayerState == PlayerStates.Alien) continue;

					return coll.gameObject;
				}

			}

			return null;
		}

		private void TryBecomingQueen()
		{
			if (disableAscension || QueenCapReached())
			{
				return;
			}

			StartCoroutine(BecomeQueen());
		}

		private IEnumerator BecomeQueen()
		{
			yield return WaitFor.Seconds(10);
			if (QueenCapReached()) yield break;
			Spawn.ServerPrefab(queenPrefab, gameObject.AssumedWorldPosServer());
			_ = Despawn.ServerSingle(gameObject);
		}

		private bool QueenCapReached()
		{
			if (queenCap < 0)
			{
				return false;
			}

			return XenoQueenAI.CurrentQueensAmt >= queenCap;
		}
	}
}
