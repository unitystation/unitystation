using UI.Minigames;
using UnityEngine;
using System;
using ScriptableObjects.MiniGames;
using Mirror;

namespace MiniGames.MiniGameModules
{
	public class ReflectionGolfModule : MiniGameModule
	{		
		public bool miniGameActive { get; private set; } = false;
		public bool miniGameWon { get; private set; } = false;

		public ReflectionGolfLevel currentLevel { get; private set; } = null;
		public GUI_ReflectionGolf miniGameGUI { get; private set; } = null;

		internal UndoInformation[] previousMoves { get; private set; } = new UndoInformation[MAX_UNDOS]; 
		private const int MAX_UNDOS = 3;

		private int expectedCellCount = 0;

		public float ScaleFactor => ReflectionGolfLevel.DISPLAY_WIDTH / Math.Max(currentLevel.Width, currentLevel.Height);

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

			currentLevel = new ReflectionGolfLevel(currentLevelName, this);

			SyncDataToClients(previousMoves, currentLevel.Width, currentLevel.LevelData);
		}

		public MiniGameResultTracker GetTracker()
		{
			return Tracker;
		}

	
		public override void StartMiniGame()
		{
			if (miniGameActive == true) return;
			miniGameActive = true;
		}

		public void InsertNewMove(Vector2Int numberLocation, Vector2Int clickLocation)
		{
			UndoInformation entry = new UndoInformation(numberLocation, clickLocation);
			previousMoves[2] = previousMoves[1];
			previousMoves[1] = previousMoves[0];
			previousMoves[0] = entry;


			if (miniGameGUI == null) return;

			miniGameGUI.UpdateGUI();
		}

		public int FetchUndoCount()
		{
			UndoInformation invalidUndo = new UndoInformation(Vector2Int.left, Vector2Int.left);
			int i = 0;

			foreach (var entry in previousMoves)
			{
				if (entry.Equals(invalidUndo) == false) i++;
			}

			return i;
		}

		public void ClearAllMoves()
		{
			previousMoves = new UndoInformation[MAX_UNDOS];
			for (int i = 0; i < MAX_UNDOS; i++)
			{
				previousMoves[i] = new UndoInformation(Vector2Int.left, Vector2Int.left);
			}
		}

		#region UIFunctions

		public void AttachGui(GUI_ReflectionGolf gui)
		{
			miniGameGUI = gui;
			UpdateCellsData();
		}
		
		public void UpdateCellsData(int _expectedCellCount)
		{
			expectedCellCount = _expectedCellCount;

			UpdateCellsData();
		}

		public void UpdateCellsData()
		{
			if (miniGameGUI == null) return;

			miniGameGUI.UpdateExpectedCellCount(expectedCellCount);
			miniGameGUI.TrimExtendCellList();		
		}

		#endregion

		#region WinLoseConditions

		protected override void OnGameDone(bool t)
		{
			miniGameActive = false;
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
			miniGameActive = false;
		}

		#endregion

		#region Networking

		[ClientRpc]
		internal void SyncDataToClients(UndoInformation[] undoInformation, short width, CellData[] levelData) //Called by server whenever it plays a valid move, or on recieving a move from a client
		{
			if (CustomNetworkManager.IsServer == true) return;

			miniGameActive = true;
			previousMoves = undoInformation;
			currentLevel = new ReflectionGolfLevel(levelData, width, this);
		}

		[Command(requiresAuthority = false)]
		internal void CmdSyncDataToSever(UndoInformation[] undoInformation, short width, CellData[] levelData, NetworkConnectionToClient sender = null) //Called by clients whenever they play a valid move on the grid
		{
			if (CustomNetworkManager.IsServer == false) return;

			if (sender == null) return;
			if (Validations.CanApply(PlayerList.Instance.Get(sender).Script, this.gameObject, NetworkSide.Server, false, ReachRange.Standard) == false) return;

			previousMoves = undoInformation;
			currentLevel = new ReflectionGolfLevel(levelData, width,this);

			SyncDataToClients(previousMoves, width,levelData);
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
			if (currentLevel == null) return;

			SyncDataToClients(previousMoves, currentLevel.Width, currentLevel.LevelData);
		}

		[ClientRpc]
		private void SyncVictory()
		{
			if (CustomNetworkManager.IsServer) return;

			if (miniGameWon == true) return; //Make sure we can only trigger this once
			miniGameWon = true;

			OnVictory();
		}

		[Command(requiresAuthority = false)]
		private void CmdSyncVictoryToServer(NetworkConnectionToClient sender = null) 
		{
			if (sender == null) return;
			if (Validations.CanApply(PlayerList.Instance.Get(sender).Script, this.gameObject, NetworkSide.Server, false, ReachRange.Standard) == false) return;

			if (miniGameWon == true) return; //Make sure we can only trigger this once
			miniGameWon = true;

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
