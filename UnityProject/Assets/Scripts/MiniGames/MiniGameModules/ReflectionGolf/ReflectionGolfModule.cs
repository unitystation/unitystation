using UI.Minigames;
using UnityEngine;
using System;
using ScriptableObjects.MiniGames;
using Mirror;

namespace MiniGames.MiniGameModules
{
	public class ReflectionGolfModule : MiniGameModule
	{		
		public bool MiniGameActive { get; private set; } = false;
		public bool MiniGameWon { get; private set; } = false;

		public ReflectionGolfLevel CurrentLevel { get; private set; } = null;

		public delegate void GuiUpdate();
		public GuiUpdate OnGuiUpdate;

		internal UndoInformation[] PreviousMoves { get; private set; } = new UndoInformation[MAX_UNDOS]; 
		private const int MAX_UNDOS = 3;

		public int ExpectedCellCount { get; private set; } = 0;

		public float ScaleFactor => ReflectionGolfLevel.DISPLAY_WIDTH / Math.Max(CurrentLevel.Width, CurrentLevel.Height);

		private string currentLevelName = "Easy 5x5 A";


		[Header("Settings"), SerializeField]
		private Difficulty selectedDifficulty = Difficulty.Normal;
		[SerializeField]
		private ReflectionGolfPuzzleList puzzleListSO = null;

		public override void OnStartClient()
		{
			base.OnStartClient();

			if (CustomNetworkManager.IsServer) return;
			RequestSync();
		}

		public override void Setup(MiniGameResultTracker tracker, GameObject parent, Difficulty difficulty = Difficulty.Normal)
		{
			base.Setup(tracker, parent);
			selectedDifficulty = difficulty;

			if (CustomNetworkManager.IsServer == false) return;

			currentLevelName = puzzleListSO.RetrieveLevel(selectedDifficulty);
			BeginLevel();
		}

		public void BeginLevel()
		{
			if (CustomNetworkManager.IsServer == false) return;
			if (currentLevelName == "") return;

			CurrentLevel = new ReflectionGolfLevel(currentLevelName, this);

			SyncDataToClients(PreviousMoves, CurrentLevel.Width, CurrentLevel.LevelData);
		}

		public MiniGameResultTracker GetTracker()
		{
			return Tracker;
		}

	
		public override void StartMiniGame()
		{
			if (MiniGameActive == true) return;
			MiniGameActive = true;
		}

		public void InsertNewMove(Vector2Int numberLocation, Vector2Int clickLocation)
		{
			UndoInformation entry = new UndoInformation(numberLocation, clickLocation);
			PreviousMoves[2] = PreviousMoves[1];
			PreviousMoves[1] = PreviousMoves[0];
			PreviousMoves[0] = entry;

			OnGuiUpdate?.Invoke();
		}

		public int FetchUndoCount()
		{
			UndoInformation invalidUndo = new UndoInformation(Vector2Int.left, Vector2Int.left);
			int i = 0;

			foreach (var entry in PreviousMoves)
			{
				if (entry.Equals(invalidUndo) == false) i++;
			}

			return i;
		}

		public void ClearAllMoves()
		{
			PreviousMoves = new UndoInformation[MAX_UNDOS];
			for (int i = 0; i < MAX_UNDOS; i++)
			{
				PreviousMoves[i] = new UndoInformation(Vector2Int.left, Vector2Int.left);
			}
		}

		#region UIFunctions
		
		public void UpdateCellsData(int _expectedCellCount)
		{
			ExpectedCellCount = _expectedCellCount;
		}

		#endregion

		#region WinLoseConditions

		protected override void OnGameDone(bool t)
		{
			MiniGameActive = false;
			base.OnGameDone(t);
		}

		public void OnFailPuzzle(GUI_ReflectionGolf gui)
		{
			if (gui == null) return;

			//We dont actually end the game for reflection golf, as you have infinite tries to get it right.

			gui.OnFail();
		}

		public void OnWinPuzzle()
		{
			if (CustomNetworkManager.IsServer)
			{
				OnVictory();
				SyncVictory();
			}
			else CmdSyncVictoryToServer();
		}

		private void OnVictory()
		{
			Tracker.OnGameEnd(true);
			MiniGameActive = false;
		}

		public void TriggerGuiUpdate()
		{
			OnGuiUpdate?.Invoke();
		}

		#endregion

		#region Networking

		[ClientRpc]
		internal void SyncDataToClients(UndoInformation[] undoInformation, short width, CellData[] levelData) //Called by server whenever it plays a valid move, or on recieving a move from a client
		{
			if (CustomNetworkManager.IsServer == true) return;

			MiniGameActive = true;
			PreviousMoves = undoInformation;
			CurrentLevel = new ReflectionGolfLevel(levelData, width, this);
		}

		[Command(requiresAuthority = false)]
		internal void CmdSyncDataToSever(UndoInformation[] undoInformation, short width, CellData[] levelData, NetworkConnectionToClient sender = null) //Called by clients whenever they play a valid move on the grid
		{
			if (CustomNetworkManager.IsServer == false) return;

			if (sender == null) return;
			if (Validations.CanApply(PlayerList.Instance.Get(sender).Script, this.gameObject, NetworkSide.Server, false, ReachRange.Standard) == false) return;

			PreviousMoves = undoInformation;
			CurrentLevel = new ReflectionGolfLevel(levelData, width,this);

			SyncDataToClients(PreviousMoves, width,levelData);
		}

		[Command(requiresAuthority = false)]
		internal void CmdReloadLevel()
		{
			if (CustomNetworkManager.IsServer == false) return;
			BeginLevel();
		}

		[Command(requiresAuthority = false)]
		internal void RequestSync() //Called by new clients in order to synchronise their puzzles
		{
			if (CurrentLevel == null) return;

			SyncDataToClients(PreviousMoves, CurrentLevel.Width, CurrentLevel.LevelData);
		}

		[ClientRpc]
		private void SyncVictory()
		{
			if (CustomNetworkManager.IsServer) return;

			if (MiniGameWon == true) return; //Make sure we can only trigger this once
			MiniGameWon = true;

			OnVictory();
		}

		[Command(requiresAuthority = false)]
		private void CmdSyncVictoryToServer(NetworkConnectionToClient sender = null) 
		{
			if (sender == null) return;
			if (Validations.CanApply(PlayerList.Instance.Get(sender).Script, this.gameObject, NetworkSide.Server, false, ReachRange.Standard) == false) return;

			if (MiniGameWon == true) return; //Make sure we can only trigger this once
			MiniGameWon = true;

			OnVictory();
			SyncVictory();
		}

		#endregion
	}


	internal struct UndoInformation
	{
		internal UndoInformation(Vector2Int _numberLocation, Vector2Int _clickLocation)
		{
			numberLocation = _numberLocation;
			clickLocation = _clickLocation;
		}

		internal Vector2Int numberLocation;
		internal Vector2Int clickLocation;
	}

	internal enum SpecialCellTypes
	{
		None = 0,
		Goal = 1,
		Wall = 2,
		Number = 3,
		Line = 12,
		TerminatedArrow = 13,
		ExpendedArrow = 14,
		ValidArrow = 15,
	}

	public enum Difficulty
	{
		Easy = 0,
		Normal = 1,
		Hard = 2,
		VeryHard = 3,
	}
}
