using System.Collections;
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
