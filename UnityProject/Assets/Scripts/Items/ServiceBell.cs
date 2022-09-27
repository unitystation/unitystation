using UnityEngine;
using AddressableReferences;
using Systems.Score;

namespace Objects
{
	public class ServiceBell : MonoBehaviour, IServerSpawn, ICheckedInteractable<HandApply>
	{

		[Tooltip("The sound the bell makes when it rings.")]
		[SerializeField] private AddressableAudioSource RingSound = null;

		[Tooltip("The additional sound for when the bell spawns as a large bell.")]
		[SerializeField]
		private AddressableAudioSource BigBellRingSound = null;

		[SerializeField] private SpriteHandler BellSpriteRenderer;

		private const string BIG_BELL_SCORE_ENTRY = "bigServiceBell";
		private const int BIG_BELL_SCORE_VALUE = 1;

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return interaction.Intent != Intent.Grab
			       && interaction.Intent != Intent.Harm
			       && interaction.TargetObject == gameObject
			       && interaction.HandObject == null;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			SoundManager.PlayNetworkedAtPos(RingSound, interaction.TargetObject.AssumedWorldPosServer());
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			// Roll for the big bell
			if (Random.value <= 0.005)
			{
				RingSound = BigBellRingSound;
				BellSpriteRenderer.ChangeSpriteVariant(1);
				ScoreMachine.AddNewScoreEntry(BIG_BELL_SCORE_ENTRY, "Number of Big Service Bells",
					ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Weird);
				ScoreMachine.AddToScoreInt(BIG_BELL_SCORE_VALUE ,BIG_BELL_SCORE_ENTRY);
			}
		}
	}
}
