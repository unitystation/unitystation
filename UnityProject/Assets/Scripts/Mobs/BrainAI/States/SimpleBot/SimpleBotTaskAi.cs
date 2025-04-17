using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using Mirror;
using NaughtyAttributes;
using Player.Language;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mobs.BrainAI.States.SimpleBot
{
	[System.Serializable]
	public struct BotDialogue
	{
		public AddressableAudioSource audioSource;
		[FormerlySerializedAs("Transcription")] public string transcription;
	}

	public class SimpleBotTaskAi : BrainMobState
	{

		[SerializeField] protected FindSimpleTaskAi findSimpleTaskAi = null;
		[SerializeField] protected AddressableAudioSource taskPerformSound;
		[SerializeField] protected AddressableAudioSource emaggedPerformSound;

		[SerializeField] protected float taskPerformDuration = 2f;

		//How far should this bot search for a target? Consider using lower values for less performant checks
		protected int searchRadius = 5;

		protected Matrix targetMatrix;
		protected Vector3Int targetCell;

		[field: SyncVar] public bool IsEmagged { get; private set; } = false;
		protected Coroutine taskPerformCoroutine = null;

		public delegate void SpriteChangeEvent(bool isEmagged, bool isPerformingTask);
		public SpriteChangeEvent OnSpriteChange = null;

		[SerializeField] protected LanguageSO botLanguage = null;
		[SerializeField] protected List<BotDialogue> stateExitDialogue = new List<BotDialogue>();


		[Button()]
		public void ToggleEmagState()
		{
			SetEmagState(!IsEmagged);
		}

		public void SetEmagState(bool state)
		{
			IsEmagged = state;
			OnSpriteChange?.Invoke(IsEmagged, false);
		}

		public override void OnEnterState()
		{
			DoTask();
		}

		public override void OnExitState()
		{
			Speak(stateExitDialogue.PickRandom());

			targetMatrix = null;
			targetCell = Vector3Int.zero;
			taskPerformCoroutine = null;

			OnSpriteChange?.Invoke(IsEmagged, false);
		}

		public void Speak(BotDialogue toSay)
		{
			Chat.AddLocalMsgToChat(toSay.transcription, gameObject, botLanguage, LivingHealthMaster.playerScript.playerName, true);

			if(toSay.audioSource != null) SoundManager.PlayNetworkedAtPosAsync(toSay.audioSource,
				LivingHealthMaster.gameObject.AssumedWorldPosServer(), global: false);
		}


		public void DoTask()
		{
			if (taskPerformCoroutine == null && IsCurrentTaskValid() == false)
			{
				master.RemoveAddState(this, findSimpleTaskAi);
				return;
			}

			if (taskPerformCoroutine is not null) return;

			OnSpriteChange?.Invoke(IsEmagged, true);
			taskPerformCoroutine = StartCoroutine(PerformTask());
		}

		public override void OnUpdateTick()
		{
			//Do nothing
		}

		protected virtual IEnumerator PerformTask()
		{
			yield return WaitFor.Seconds(taskPerformDuration);

			taskPerformCoroutine = null;

			DoTask();
		}

		protected virtual bool IsCurrentTaskValid()
		{
			return false;
		}

		public virtual bool FindTarget(out Vector3Int targetPosition, out Matrix targetMatrix)
		{
			targetPosition = Vector3Int.zero;
			targetMatrix = null;
			return false;
		}


		public override bool HasGoal()
		{
			return targetMatrix;
		}
	}
}