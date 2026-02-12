using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Core.Sprite_Handler;
using US13.Managers;
using US13.Systems.Score;
using US13.Systems.Score.ScoreEntry;
using US13.UI.Systems.MainHUD.UI_Bottom;
using Util;

namespace US13.Items
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
				BellSpriteRenderer.SetSpriteVariant(1);
				ScoreMachine.AddNewScoreEntry(BIG_BELL_SCORE_ENTRY, "Number of Big Service Bells",
					ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Weird);
				ScoreMachine.AddToScoreInt(BIG_BELL_SCORE_VALUE ,BIG_BELL_SCORE_ENTRY);
			}
		}
	}
}
