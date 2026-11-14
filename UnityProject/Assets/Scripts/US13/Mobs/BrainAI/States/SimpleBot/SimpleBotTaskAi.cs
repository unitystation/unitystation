using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Chat;
using US13.Managers;
using US13.Player.Language;
using US13.Tilemaps.Behaviours.Layers;
using Util;

namespace US13.Mobs.BrainAI.States.SimpleBot
{
	[System.Serializable]
	public class SimpleBotTaskAi : BrainMobState
	{
		[UnityEngine.SerializeReference] protected FindSimpleTaskAi findSimpleTaskAi = null;
		[SerializeField] protected AddressableAudioSource taskPerformSound;
		[SerializeField] protected AddressableAudioSource emaggedPerformSound;

		[SerializeField] protected float taskPerformDuration = 2f;

		//How far should this bot search for a target? Consider using lower values for less performant checks
		protected int searchRadius = 5;

		protected Matrix targetMatrix;
		protected Vector3Int targetCell;

		[SerializeField, UnityEngine.Serialization.FormerlySerializedAs("IsEmagged")] private bool _networkIsEmagged = false;
		public bool IsEmagged
		{
			get => _networkIsEmagged;
			private set
			{
				if (System.Collections.Generic.EqualityComparer<bool>.Default.Equals(_networkIsEmagged, value)) return;
				_networkIsEmagged = value;
			}
		}


		public delegate void SpriteChangeEvent(bool isEmagged, bool isPerformingTask);
		public SpriteChangeEvent OnSpriteChange = null;

		[SerializeField] protected LanguageSO botLanguage = null;
		[SerializeField] protected List<AudibleMobDialogue> stateExitDialogue = new List<AudibleMobDialogue>();
		[SerializeField] protected List<AudibleMobDialogue> stateEnterDialogue = new List<AudibleMobDialogue>();

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
			Speak(stateEnterDialogue.PickRandom());
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

		public virtual List<Vector3Int> FindTarget(out Vector3Int targetPosition, out Matrix targetMatrix)
		{
			targetPosition = Vector3Int.zero;
			targetMatrix = null;
			return null;
		}


		public override bool HasGoal()
		{
			return targetMatrix;
		}
		[System.Serializable]
		private sealed class BehaviorNetworkState
		{
			public bool IsEmagged;
		}

		public override string CaptureNetworkState() => JsonUtility.ToJson(new BehaviorNetworkState
		{
			IsEmagged = this.IsEmagged,
		});

		public override void ApplyNetworkState(string json)
		{
			var state = JsonUtility.FromJson<BehaviorNetworkState>(json);
			IsEmagged = state.IsEmagged;
		}

	}
}
