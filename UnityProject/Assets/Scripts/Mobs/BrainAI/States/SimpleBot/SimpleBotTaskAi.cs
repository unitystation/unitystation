using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using AddressableReferences;
using Cysharp.Threading.Tasks;
using Mirror;
using NaughtyAttributes;
using Player.Language;
using UnityEngine;
using Mobs;

namespace Mobs.BrainAI.States.SimpleBot
{
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


		public delegate void SpriteChangeEvent(bool isEmagged, bool isPerformingTask);
		public SpriteChangeEvent OnSpriteChange = null;

		[SerializeField] protected LanguageSO botLanguage = null;
		[SerializeField] protected List<AudibleMobDialogue> stateExitDialogue = new List<AudibleMobDialogue>();

		protected CancellationTokenSource cancellationTokenSource = new();
		protected bool isPerformingTask = false;

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
			isPerformingTask = false;

			OnSpriteChange?.Invoke(IsEmagged, false);
		}

		public void Speak(AudibleMobDialogue toSay)
		{
			if (string.IsNullOrWhiteSpace(LivingHealthMaster?.playerScript?.playerName)) return;
			Chat.AddLocalMsgToChat(toSay.transcription, gameObject, botLanguage, LivingHealthMaster.playerScript.playerName, true);

			if(toSay.audioSource != null) SoundManager.PlayNetworkedAtPosAsync(toSay.audioSource,
				LivingHealthMaster.gameObject.AssumedWorldPosServer(), global: false);
		}


		public void DoTask()
		{
			if (isPerformingTask == false && IsCurrentTaskValid() == false)
			{
				master.RemoveAddState(this, findSimpleTaskAi);
				return;
			}

			if (isPerformingTask == true) return;

			OnSpriteChange?.Invoke(IsEmagged, true);
			_ = PerformTask();
		}

		public override void OnUpdateTick()
		{
			//Do nothing
		}

		protected virtual async UniTask PerformTask()
		{
			isPerformingTask = true;

			bool isCancelled = await UniTask.Delay(TimeSpan.FromSeconds(taskPerformDuration), cancellationToken: cancellationTokenSource.Token).SuppressCancellationThrow();
			isPerformingTask = false;

			if (isCancelled) return;
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