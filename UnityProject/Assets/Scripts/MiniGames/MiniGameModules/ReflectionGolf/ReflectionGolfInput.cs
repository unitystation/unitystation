using MiniGames.MiniGameModules;
using UnityEngine;
using System.Drawing;
using System;
using UI.Minigames;
using Clothing;

public class ReflectionGolfInput
{
	private GUI_ReflectionGolf gui = null;
	private ReflectionGolfModule miniGameModule => gui.miniGameModule;

	private ReflectionGolfLevel level => miniGameModule.currentLevel;
	private float cellSize => miniGameModule.ScaleFactor;

	private RectTransform gridTransform = null;

	private Vector2Int previousGridClick = Vector2Int.zero;

	private bool isGameActive => miniGameModule.miniGameActive;

	private int MAX_RECURSION = 20; //This can be increased if needed, just here to prevent infinite loops in situtation wehere things go wrong.

	public bool initialised { get; private set; } = false;

	public ReflectionGolfInput(RectTransform transform)
	{
		gridTransform = transform;
	}

	public void AttachGUI(GUI_ReflectionGolf module) //Each GUI instance has its own input controller object, but each GolfModule can have multiple GUIs. More reliable to use the GUI here instead of the Module as a result
	{
		gui = module;
		initialised = true;
	}

	public void OnGridPress(Vector3 mousePosition, Vector3 _uiPosition)
	{
		if (initialised == false || isGameActive == false) return;

		float uiscale = UIManager.Instance.Scaler.scaleFactor;
		float _cellSize = cellSize * uiscale;

		Vector2 pos = (mousePosition - _uiPosition).To2() - gridTransform.anchoredPosition + new Vector2(300 * uiscale, 300 * uiscale); //To local coordinate space


		if (level.Width < level.Height) pos.x = pos.x - ((level.Height - level.Width) * _cellSize / 2);
		else if(level.Height < level.Width) pos.y = pos.y - ((level.Width - level.Height) * _cellSize / 2);

		Vector2Int clickedGridPosition = new Vector2Int((int)(pos.x / _cellSize), (int)(pos.y / _cellSize));


		if (pos.x < 0 || pos.y < 0 || clickedGridPosition.x < 0 || clickedGridPosition.x > level.Width - 1 || clickedGridPosition.y < 0 || clickedGridPosition.y > level.Height - 1)
		{
			OnFalseMove();
			gui.UpdateCellSpriteColour();	
			return;
		}

		if (level.LevelData[clickedGridPosition.x + clickedGridPosition.y*level.Width].isNumber == true) OnNumberClick(clickedGridPosition);
		else OnNonNumberClick(clickedGridPosition);

		gui.UpdateCellSpriteColour();
	}

	private void OnNumberClick(Vector2Int clickPosition) //If we click on a number, select that number, and if the previous click was also a number, deselect it.
	{
		if (previousGridClick != Vector2Int.left && previousGridClick != clickPosition)
		{
			CellData oldCellData = level.LevelData[previousGridClick.x + previousGridClick.y*level.Width];

			if (oldCellData.isTouched == true) oldCellData.isTouched = false;
	
			level.LevelData[previousGridClick.x + previousGridClick.y * level.Width] = oldCellData;		
		}

		CellData newCellData = level.LevelData[clickPosition.x + clickPosition.y*level.Width];
		newCellData.isTouched = !newCellData.isTouched;

		level.LevelData[clickPosition.x + clickPosition.y*level.Width] = newCellData;

		previousGridClick = clickPosition;
	}

	private void OnNonNumberClick(Vector2Int clickPosition)
	{
		if (previousGridClick == Vector2Int.left)
		{
			previousGridClick = clickPosition;
			return;
		}

		int indexOld = previousGridClick.x + previousGridClick.y * level.Width;
		CellData oldCellData = level.LevelData[indexOld];

		if (oldCellData.isNumber == false || oldCellData.isTouched == false)
		{
			previousGridClick = clickPosition;
			return; //Unless we enabled a number with our last click, do nothing
		}

		level.LevelData[indexOld] = oldCellData;
		
		Vector2Int extensionDirection;

		if (Math.Abs(clickPosition.x - previousGridClick.x) > Math.Abs(clickPosition.y - previousGridClick.y)) extensionDirection = new Vector2Int(Math.Sign(clickPosition.x - previousGridClick.x), 0);
		else extensionDirection = new Vector2Int(0, Math.Sign(clickPosition.y - previousGridClick.y));

		int lineLength = ExtendNumber(extensionDirection);

		if (lineLength == 0)
		{
			level.LevelData[indexOld] = oldCellData;
			return;
		}
		
		oldCellData.isNumber = false;
		oldCellData.isTouched = false; //Disable a number after it has moved

		level.LevelData[indexOld] = oldCellData;

		gui.UpdateExpectedCellCount(gui.expectedCellCount + lineLength);
		miniGameModule.InsertNewMove(previousGridClick, clickPosition);

		if (CustomNetworkManager.IsServer) miniGameModule.SyncDataToClients(miniGameModule.previousMoves, level.Width, level.LevelData);
		else miniGameModule.CmdSyncDataToSever(miniGameModule.previousMoves, level.Width, level.LevelData);
		
	}

	internal void OnUndo()
	{
		if (miniGameModule.previousMoves[0].numberLocation == Vector2Int.left || miniGameModule.previousMoves[0].clickLocation == Vector2Int.left) return;

		UndoLine(miniGameModule.previousMoves[0]); //Actually performs the Undo action on the grid

		UndoInformation invalidEntry = new UndoInformation(Vector2Int.left, Vector2Int.left);

		miniGameModule.previousMoves[0] = miniGameModule.previousMoves[1]; //Updates the undo array to remove the top most entry
		miniGameModule.previousMoves[1] = miniGameModule.previousMoves[2];
		miniGameModule.previousMoves[2] = invalidEntry;

		if (CustomNetworkManager.IsServer) miniGameModule.SyncDataToClients(miniGameModule.previousMoves, level.Width, level.LevelData); //Syncs the new grid data and undos to all clients
		else miniGameModule.CmdSyncDataToSever(miniGameModule.previousMoves, level.Width, level.LevelData);

		gui.UpdateGUI();
	}

	private void UndoLine(UndoInformation undo)
	{
		int numberIndex = undo.numberLocation.x + undo.numberLocation.y * level.Width;
		level.LevelData[numberIndex].isNumber = true;
		level.LevelData[numberIndex].isTouched = false;

		Vector2Int extensionDirection;
		Vector2Int clickPosition = undo.clickLocation;
		Vector2Int numPos = undo.numberLocation;

		if (Math.Abs(clickPosition.x - numPos.x) > Math.Abs(clickPosition.y - numPos.y)) extensionDirection = new Vector2Int(Math.Sign(clickPosition.x - numPos.x), 0);
		else extensionDirection = new Vector2Int(0, Math.Sign(clickPosition.y - numPos.y));

		int lineLength = 0;

		for (int i = 0; i < MAX_RECURSION; i++)
		{
			Vector2Int newGridPosition = numPos + (extensionDirection * (i + 1));
			int indexNew = newGridPosition.x + newGridPosition.y * level.Width;
			int oldVal = level.LevelData[indexNew].value;

			level.LevelData[indexNew].value = (int)SpecialCellTypes.None;
			level.LevelData[indexNew].currentRotation = 0;
			level.LevelData[indexNew].isNumber = false;

			lineLength++;

			if (oldVal >= (int)SpecialCellTypes.TerminatedArrow || i == MAX_RECURSION - 1)
			{
				int goalIndex = level.GoalLocation.x + level.GoalLocation.y * level.Width;
				level.LevelData[goalIndex].value = (int)SpecialCellTypes.Goal;

				break;
			}

		}

		gui.UpdateExpectedCellCount(gui.expectedCellCount - lineLength);
	}

	private int ExtendNumber(Vector2Int extensionDirection)
	{
		int lineLength = 0;
		int expectedLineLength = GetExpectedLineLength(previousGridClick);
		bool puzzleFailed = false;

		for (int i = 0; i < MAX_RECURSION; i++)
		{
			Vector2Int newGridPosition = previousGridClick + (extensionDirection * (i + 1));
			int indexNew = newGridPosition.x + newGridPosition.y * level.Width;

			bool outOfBounds = newGridPosition.x < 0 || newGridPosition.x > level.Width - 1 || newGridPosition.y < 0 || newGridPosition.y > level.Height - 1;

			if (outOfBounds || level.LevelData[indexNew].value > (int)SpecialCellTypes.Goal || i == MAX_RECURSION - 1)
			{
				if (i == 0) break; //If this is the first segment, dont create any sort of line

				Vector2Int priorGridPosition = previousGridClick + (extensionDirection * i);
				int indexPrior = priorGridPosition.x + priorGridPosition.y * level.Width;

				if (priorGridPosition == level.GoalLocation) //Line terminated in a goal
				{
					puzzleFailed = false;
					miniGameModule.OnWinPuzzle();
				}
				if (lineLength == expectedLineLength)
				{
					level.LevelData[indexPrior].value = (short)(SpecialCellTypes.ValidArrow + expectedLineLength - 1);
					level.LevelData[indexPrior].isNumber = true;
					break;
				}
				level.LevelData[indexPrior].value = (short)SpecialCellTypes.TerminatedArrow;

				break;
			}

			if (level.LevelData[indexNew].value == (int)SpecialCellTypes.Goal) puzzleFailed = true;

			level.LevelData[indexNew].value = (int)SpecialCellTypes.Line;
			level.LevelData[indexNew].currentRotation = Vector2IntToDirectionShort(extensionDirection);
			level.LevelData[indexNew].isNumber = false;

			lineLength++;
		}

		if(puzzleFailed) miniGameModule.OnFailPuzzle(gui);

		return lineLength;
	}

	private void OnFalseMove()
	{
		if (previousGridClick == Vector2Int.left) return; //Last move was also invalid

		level.LevelData[previousGridClick.x + previousGridClick.y*level.Width].isTouched = false; //Deselect any select numbers if clicked out of bounds

		previousGridClick = Vector2Int.left;
	}

	private short Vector2IntToDirectionShort(Vector2Int direction)
	{
		if (direction.y != 0) return (short)(1 - direction.y);
		else return (short)Math.Sign(-direction.x);
	}

	private int GetExpectedLineLength(Vector2Int numberLocation)
	{
		int val = level.LevelData[numberLocation.x + numberLocation.y*level.Width].value;

		if (val == (int)SpecialCellTypes.Wall || val == (int)SpecialCellTypes.Line || val == (int)SpecialCellTypes.TerminatedArrow || val == (int)SpecialCellTypes.ExpendedArrow) return 0;

		if (val >= (int)SpecialCellTypes.ValidArrow) return val - (int)SpecialCellTypes.ValidArrow + 1;
		else return val - (int)SpecialCellTypes.Number + 1;
	}
}