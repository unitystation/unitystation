using UI.Minigames;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace MiniGames.MiniGameModules
{
	public class ReflectionGolfModule : MiniGameModule
	{
		/// <summary>
		/// NOTE TO SELF, NETWORK currentLevel.LevelData and previousMoves in order to sync
		/// </summary>
		
		public bool miniGameActive { get; private set; } = false;

		public ReflectionGolfLevel currentLevel { get; private set; } = null;
		private GUI_ReflectionGolf miniGameGUI = null;

		private UndoInformation[] previousMoves = new UndoInformation[MAX_UNDOS]; 
		private const int MAX_UNDOS = 3;

		private int expectedCellCount = 0;

		public float ScaleFactor => ReflectionGolfLevel.DISPLAY_WIDTH / Math.Max(currentLevel.Width, currentLevel.Height);



		public override void Setup(MiniGameResultTracker tracker, GameObject parent)
		{
			base.Setup(tracker, parent);
		}

		public override void StartMiniGame()
		{
			if (miniGameActive) return;
			currentLevel = new ReflectionGolfLevel("", this);

			miniGameActive = true;
		}

		protected override void OnGameDone(bool t)
		{
			miniGameActive = false;
			base.OnGameDone(t);
		}

		public void AttachGui(GUI_ReflectionGolf gui)
		{
			miniGameGUI = gui;
			UpdateCellsData();

		}

		public int GetValueAtCell(Vector2Int gridPosition)
		{
			if (gridPosition.x < 0 || gridPosition.y < 0 || gridPosition.x > currentLevel.Width - 1 || gridPosition.y > currentLevel.Height - 1) return (int)SpecialCellTypes.Wall;

			return currentLevel.LevelData[gridPosition.x, gridPosition.y].value;
		}

		
		public void UpdateCellsData(int _expectedCellCount)
		{
			expectedCellCount += _expectedCellCount;

			UpdateCellsData();
		}

		public void UpdateCellsData()
		{
			if (miniGameGUI == null) return;

			miniGameGUI.UpdateExpectedCellCount(expectedCellCount);
			miniGameGUI.TrimExtendCellList();
		}

		public void RegisterNewLine()
		{
			UpdateCellsData(1);
		}

		public void InsertNewMove(Vector2Int numberLocation, Vector2Int clickLocation)
		{
			UndoInformation entry = new UndoInformation(numberLocation, clickLocation); 
			previousMoves[2] = previousMoves[1];
			previousMoves[1] = previousMoves[0];
			previousMoves[0] = entry;
			

			if (miniGameGUI == null) return;

			miniGameGUI.UpdateCellSprites();
		}

		public void ClearAllMoves()
		{
			previousMoves = new UndoInformation[MAX_UNDOS];
		}

		public void AlterCurrentLevelValue(Vector2Int gridPosition, short newValue)
		{
			if (gridPosition.x < 0 || gridPosition.y < 0 || gridPosition.x > currentLevel.Width - 1 || gridPosition.y > currentLevel.Height - 1) return;

			currentLevel.LevelData[gridPosition.x, gridPosition.y].value = newValue;
		}

		public void AlterCurrentLevelRotation(Vector2Int gridPosition, short newValue)
		{
			if (gridPosition.x < 0 || gridPosition.y < 0 || gridPosition.x > currentLevel.Width - 1 || gridPosition.y > currentLevel.Height - 1) return;

			currentLevel.LevelData[gridPosition.x, gridPosition.y].currentRotation = newValue;
		}

		public void AlterCurrentLevelEnabled(Vector2Int gridPosition, bool newValue)
		{
			if (gridPosition.x < 0 || gridPosition.y < 0 || gridPosition.x > currentLevel.Width - 1 || gridPosition.y > currentLevel.Height - 1) return;

			currentLevel.LevelData[gridPosition.x, gridPosition.y].isTouched = newValue;

			if (miniGameGUI == null) return;

			miniGameGUI.UpdateCellSpriteColour();

		}

		public void OnFailPuzzle()
		{
			if (miniGameGUI == null) return;

			miniGameGUI.UpdateExpectedCellCount(miniGameGUI.expectedCellCount - 1); //A line will be over the goal;
		}

		public void OnWinPuzzle()
		{
			if (miniGameGUI == null) return;

			miniGameGUI.UpdateExpectedCellCount(miniGameGUI.expectedCellCount - 1); //A line will be over the goal;
		}
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
}
