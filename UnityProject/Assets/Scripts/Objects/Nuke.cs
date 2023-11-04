using System;
using System.Collections;
using Audio.Managers;
using UnityEngine;
using Mirror;
using UnityEngine.Events;
using AddressableReferences;
using Antagonists;
using HealthV2;
using Managers;
using Systems.Score;
using UI.Chat_UI;
using Random = UnityEngine.Random;

namespace Objects.Command
{
	/// <summary>
	/// Main component for nuke.
	/// </summary>
	public class Nuke : NetworkBehaviour, ICheckedInteractable<HandApply>, IAdminInfo, IServerLifecycle
	{
		public NukeTimerEvent OnTimerUpdate = new NukeTimerEvent();

		[SerializeField] private AddressableAudioSource TimerTickSound = null;

		private UniversalObjectPhysics objectBehaviour;
		private ItemStorage itemNuke;
		private Coroutine timerHandle;
		private CentComm.AlertLevel CurrentAlertLevel;
		private ItemSlot nukeSlot;

		public ItemSlot NukeSlot => nukeSlot;

		[SerializeField]
		private bool isAncharable = true;

		public bool IsAncharable => isAncharable;

		private bool isSafetyOn = true;


		private bool isCodeRight;

		public bool IsCodeRight => isCodeRight;

		public float explosionRadius = 1500;
		[SerializeField]
		private int minTimer = 270;
		private bool isTimer;

		public bool IsTimer => isTimer;

		private bool isTimerTicking;

		private int currentTimerSeconds;
		public int CurrentTimerSeconds {
			get => currentTimerSeconds;
			private set {
				currentTimerSeconds = value;
				OnTimerUpdate.Invoke(currentTimerSeconds);
			}
		}

		public static bool Detonated;

		private string currentCode = "";
		public string CurrentCode => currentCode;

		//Nuke code is only populated on the server
		private int nukeCode;
		public int NukeCode => nukeCode;

		private const string ON_NUKE_SCORE_ENTRY = "nukedStation";
		private const int ON_NUKE_SCORE_VALUE = -550000;

		private void Awake()
		{
			currentTimerSeconds = minTimer;
			objectBehaviour = GetComponent<UniversalObjectPhysics>();
			itemNuke = GetComponent<ItemStorage>();
			nukeSlot = itemNuke.GetIndexedItemSlot(0);
			Detonated = false;
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if (SubSceneManager.Instance.SyndicateScene == gameObject.scene)
			{
				nukeCode = AntagManager.SyndiNukeCode;
			}
			else
			{
				nukeCode = CodeGenerator();
			}
		}

		private void OnDisable()
		{
			//Stop nuke detonating after round end or if its been destroyed!
			StopAllCoroutines();
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			//Stop nuke detonating after round end or if its been destroyed!
			StopAllCoroutines();
		}

		public static int CodeGenerator()
		{
			return Random.Range(1000, 9999);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side))
				return false;

			//interaction only works if using an ID card on console
			if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.NukeDisk))
			{ return false; }

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			Inventory.ServerTransfer(interaction.HandSlot, nukeSlot);
		}

		#region Detonation
		[Server]
		private void Detonate()
		{
			if ((gameObject.AssumedWorldPosServer() - MatrixManager.MainStationMatrix.GameObject.AssumedWorldPosServer()).magnitude < explosionRadius)
			{
				Detonated = true;
				//if yes, blow up the nuke
				RpcDetonate();
				//Kill Everyone in the universe
				//FIXME kill only people on the station matrix that the nuke was detonated on
				StartCoroutine(WaitForDeath());
				GameManager.Instance.RespawnCurrentlyAllowed = false;
				DetonateVideo();
				ScoreMachine.AddNewScoreEntry(ON_NUKE_SCORE_ENTRY, "Station Nuked",
					ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Bad);
				ScoreMachine.AddToScoreInt(ON_NUKE_SCORE_VALUE, ON_NUKE_SCORE_ENTRY);
			}
			else
			{
				GameManager.Instance.EndRound();
			}
		}

		//Server telling the nukes to explode
		[ClientRpc]
		void RpcDetonate()
		{
			DetonateVideo();
		}

		void DetonateVideo()
		{
			SoundAmbientManager.StopAllAudio();
			//turning off all the UI except for the right panel
			UIManager.PlayerHealthUI.gameObject.SetActive(false);
			UIManager.Display.hudBottomHuman.gameObject.SetActive(false);
			UIManager.Display.hudBottomGhost.gameObject.SetActive(false);
			ChatUI.Instance.CloseChatWindow();

			//Playing the video
			UIManager.Display.VideoPlayer.PlayNukeDetVideo();
		}

		#endregion


		#region Buttons related

		/// <summary>
		/// Tries to add new digit to code input
		/// </summary>
		/// <param name="c"></param>
		/// <returns>true if digit is appended ok</returns>
		public bool AppendKey(char c)
		{
			int digit;
			if (int.TryParse(c.ToString(), out digit) && currentCode.Length < nukeCode.ToString().Length)
			{
				currentCode = CurrentCode + digit;
				return true;
			}
			return false;
		}

		[Server]
		public bool? SafetyNuke()
		{
			if (!isSafetyOn)
			{
				if (isTimer)
				{
					if (isTimerTicking)
					{
						GameManager.Instance.CentComm.lastAlertChange = GameManager.Instance.RoundTime;
						GameManager.Instance.CentComm.ChangeAlertLevel(CurrentAlertLevel);
						this.TryStopCoroutine(ref timerHandle);
						isTimerTicking = false;
					}
					isTimer = false;
				}
				isSafetyOn = !isSafetyOn;
				return isSafetyOn;
			}
			else if (isCodeRight)
			{
				isSafetyOn = !isSafetyOn;
				return isSafetyOn;
			}
			return null;
		}

		[Server]
		public bool? AnchorNuke()
		{
			if (IsCodeRight && !isSafetyOn)
			{
				bool isPushable = !objectBehaviour.IsNotPushable;
				objectBehaviour.SetIsNotPushable(isPushable);
				return isPushable;
			}
			return null;
		}

		public bool? ToggleTimer()
		{
			if (IsCodeRight && !isSafetyOn)
			{
				if (isTimer && isTimerTicking)
				{
					isTimerTicking = false;
					GameManager.Instance.CentComm.lastAlertChange = GameManager.Instance.RoundTime;
					GameManager.Instance.CentComm.ChangeAlertLevel(CurrentAlertLevel);
					this.TryStopCoroutine(ref timerHandle);
				}
				isTimer = !isTimer;
				return isTimer;
			}
			return null;
		}

		//Server validating the code sent back by the GUI
		[Server]
		public bool? Validate()
		{
			if (isCodeRight && isTimer && !isTimerTicking)
			{
				if (currentCode == "")
				{
					return false;
				}
				int digit = int.Parse(currentCode);
				if (digit < minTimer)
				{
					return false;
				}
				isTimerTicking = true;
				CurrentTimerSeconds = digit;
				CurrentAlertLevel = GameManager.Instance.CentComm.CurrentAlertLevel;
				GameManager.Instance.CentComm.lastAlertChange = GameManager.Instance.RoundTime;
				GameManager.Instance.CentComm.ChangeAlertLevel(CentComm.AlertLevel.Delta);
				this.StartCoroutine(TickTimer(), ref timerHandle);
				return true;
			}
			if (!isCodeRight)
			{
				isCodeRight = CurrentCode == NukeCode.ToString();
				return isCodeRight;
			}
			return null;
		}

		public void EjectDisk()
		{
			if (!nukeSlot.IsEmpty)
			{
				Inventory.ServerDrop(nukeSlot);
				isCodeRight = false;
				Clear();
			}
		}

		public void Clear()
		{
			currentCode = "";
		}

		#endregion

		IEnumerator WaitForDeath()
		{
			yield return WaitFor.Seconds(2.5f);
			var worldPos = gameObject.GetComponent<RegisterTile>().WorldPosition;
			foreach (LivingHealthMasterBase livingHealth in FindObjectsOfType<LivingHealthMasterBase>())
			{
				var dist = Vector3.Distance(worldPos, livingHealth.GetComponent<RegisterTile>().WorldPosition);
				if (dist < explosionRadius)
				{
					livingHealth.Death();
				}
			}
			yield return WaitFor.Seconds(10f);
			// Trigger end of round
			GameManager.Instance.RoundEndTime = 10;
			GameManager.Instance.EndRound();

		}

		IEnumerator TickTimer()
		{
			while (CurrentTimerSeconds > 0)
			{
				CurrentTimerSeconds -= 1;
				SoundManager.PlayNetworkedAtPos(TimerTickSound, gameObject.AssumedWorldPosServer());
				yield return WaitFor.Seconds(1);
			}
			Detonate();
		}
		public string AdminInfoString()
		{
			return $"Nuke Code: {nukeCode}";
		}
	}
	public class NukeTimerEvent : UnityEvent<int> { }
}
