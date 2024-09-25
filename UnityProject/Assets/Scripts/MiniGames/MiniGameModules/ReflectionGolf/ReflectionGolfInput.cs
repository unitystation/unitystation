using MiniGames.MiniGameModules;
using UnityEngine;
using System;
using UI.Minigames;

public class ReflectionGolfInput
{
	private GUI_ReflectionGolf gui = null;

	private ReflectionGolfModule MiniGameModule => gui.MiniGameModule;
	private float CellSize => MiniGameModule.ScaleFactor;
	private bool IsGameActive => MiniGameModule.MiniGameActive;

	private ReflectionGolfLevel Level => MiniGameModule.CurrentLevel;

	private readonly RectTransform gridTransform = null;

	private Vector2Int previousGridClick = Vector2Int.zero;

	private const int MAX_RECURSION = 20; //This can be increased if needed, just here to prevent infinite loops in situtation wehere things go wrong.

	public bool Initialised { get; private set; } = false;


	public ReflectionGolfInput(RectTransform transform)
	{
		gridTransform = transform;
	}

	public void AttachGui(GUI_ReflectionGolf module) //Each GUI instance has its own input controller object, but each GolfModule can have multiple GUIs. More reliable to use the GUI here instead of the Module as a result
	{
		gui = module;
		Initialised = true;
	}

	public void OnGridPress(Vector3 mousePosition, Vector3 _uiPosition)
	{
		if (Initialised == false || IsGameActive == false) return;

		float uiscale =  UIManager.Instance.Scaler.scaleFactor;
		float _cellSize = CellSize * uiscale;

		Vector2 pos = (mousePosition - _uiPosition).To2() - gridTransform.anchoredPosition*uiscale + new Vector2(300 * uiscale, 300 * uiscale); //To local coordinate space


		if (Level.Width < Level.Height) pos.x = pos.x - ((Level.Height - Level.Width) * _cellSize / 2);
		else if(Level.Height < Level.Width) pos.y = pos.y - ((Level.Width - Level.Height) * _cellSize / 2);

		Vector2Int clickedGridPosition = new Vector2Int((int)(pos.x / _cellSize), (int)(pos.y / _cellSize));


		if (pos.x < 0 || pos.y < 0 || clickedGridPosition.x < 0 || clickedGridPosition.x > Level.Width - 1 || clickedGridPosition.y < 0 || clickedGridPosition.y > Level.Height - 1)
		{
			OnFalseMove();
			gui.UpdateCellSpriteColour();	
			return;
		}

		if (Level.LevelData[clickedGridPosition.x + clickedGridPosition.y*Level.Width].isNumber == true) OnNumberClick(clickedGridPosition);
		else OnNonNumberClick(clickedGridPosition);

		gui.UpdateCellSpriteColour();
	}

	private void OnNumberClick(Vector2Int clickPosition) //If we click on a number, select that number, and if the previous click was also a number, deselect it.
	{
		if (previousGridClick != Vector2Int.left && previousGridClick != clickPosition)
		{
			CellData oldCellData = Level.LevelData[previousGridClick.x + previousGridClick.y*Level.Width];

			if (oldCellData.isTouched == true) oldCellData.isTouched = false;
	
			Level.LevelData[previousGridClick.x + previousGridClick.y * Level.Width] = oldCellData;		
		}

		CellData newCellData = Level.LevelData[clickPosition.x + clickPosition.y*Level.Width];
		newCellData.isTouched = !newCellData.isTouched;

		Level.LevelData[clickPosition.x + clickPosition.y*Level.Width] = newCellData;

		previousGridClick = clickPosition;
	}

	private void OnNonNumberClick(Vector2Int clickPosition)
	{
		if (previousGridClick == Vector2Int.left)
		{
			previousGridClick = clickPosition;
			return;
		}

		int indexOld = previousGridClick.x + previousGridClick.y * Level.Width;
		CellData oldCellData = Level.LevelData[indexOld];

		if (oldCellData.isNumber == false || oldCellData.isTouched == false)
		{
			previousGridClick = clickPosition;
			return; //Unless we enabled a number with our last click, do nothing
		}

		Level.LevelData[indexOld] = oldCellData;
		
		Vector2Int extensionDirection;

		if (Math.Abs(clickPosition.x - previousGridClick.x) > Math.Abs(clickPosition.y - previousGridClick.y)) extensionDirection = new Vector2Int(Math.Sign(clickPosition.x - previousGridClick.x), 0);
		else extensionDirection = new Vector2Int(0, Math.Sign(clickPosition.y - previousGridClick.y));

		int lineLength = ExtendNumber(extensionDirection);

		if (lineLength == 0)
		{
			Level.LevelData[indexOld] = oldCellData;
			return;
		}
		
		oldCellData.isNumber = false;
		oldCellData.isTouched = false; //Disable a number after it has moved

		Level.LevelData[indexOld] = oldCellData;

		MiniGameModule.UpdateCellsData(MiniGameModule.ExpectedCellCount + lineLength);
		MiniGameModule.InsertNewMove(previousGridClick, clickPosition);

		if (CustomNetworkManager.IsServer) MiniGameModule.SyncDataToClients(MiniGameModule.PreviousMoves, Level.Width, Level.LevelData);
		else MiniGameModule.CmdSyncDataToSever(MiniGameModule.PreviousMoves, Level.Width, Level.LevelData);
		
	}

	internal void OnUndo()
	{
		if (MiniGameModule.PreviousMoves[0].numberLocation == Vector2Int.left || MiniGameModule.PreviousMoves[0].clickLocation == Vector2Int.left) return;

		UndoLine(MiniGameModule.PreviousMoves[0]); //Actually performs the Undo action on the grid

		UndoInformation invalidEntry = new UndoInformation(Vector2Int.left, Vector2Int.left);

		MiniGameModule.PreviousMoves[0] = MiniGameModule.PreviousMoves[1]; //Updates the undo array to remove the top most entry
		MiniGameModule.PreviousMoves[1] = MiniGameModule.PreviousMoves[2];
		MiniGameModule.PreviousMoves[2] = invalidEntry;

		if (CustomNetworkManager.IsServer) MiniGameModule.SyncDataToClients(MiniGameModule.PreviousMoves, Level.Width, Level.LevelData); //Syncs the new grid data and undos to all clients
		else MiniGameModule.CmdSyncDataToSever(MiniGameModule.PreviousMoves, Level.Width, Level.LevelData);

		gui.UpdateGui();
	}

	private void UndoLine(UndoInformation undo)
	{
		int numberIndex = undo.numberLocation.x + undo.numberLocation.y * Level.Width;
		Level.LevelData[numberIndex].isNumber = true;
		Level.LevelData[numberIndex].isTouched = false;

		Vector2Int extensionDirection;
		Vector2Int clickPosition = undo.clickLocation;
		Vector2Int numPos = undo.numberLocation;

		if (Math.Abs(clickPosition.x - numPos.x) > Math.Abs(clickPosition.y - numPos.y)) extensionDirection = new Vector2Int(Math.Sign(clickPosition.x - numPos.x), 0);
		else extensionDirection = new Vector2Int(0, Math.Sign(clickPosition.y - numPos.y));

		int lineLength = 0;

		for (int i = 0; i < MAX_RECURSION; i++)
		{
			Vector2Int newGridPosition = numPos + (extensionDirection * (i + 1));
			int indexNew = newGridPosition.x + newGridPosition.y * Level.Width;
			int oldVal = Level.LevelData[indexNew].value;

			Level.LevelData[indexNew].value = (int)SpecialCellTypes.None;
			Level.LevelData[indexNew].currentRotation = 0;
			Level.LevelData[indexNew].isNumber = false;

			lineLength++;

			if (oldVal >= (int)SpecialCellTypes.TerminatedArrow || i == MAX_RECURSION - 1)
			{
				int goalIndex = Level.GoalLocation.x + Level.GoalLocation.y * Level.Width;
				Level.LevelData[goalIndex].value = (int)SpecialCellTypes.Goal;

				break;
			}

		}

		MiniGameModule.UpdateCellsData(MiniGameModule.ExpectedCellCount - lineLength);
	}

	private int ExtendNumber(Vector2Int extensionDirection)
	{
		int lineLength = 0;
		int expectedLineLength = GetExpectedLineLength(previousGridClick);
		bool puzzleFailed = false;

		for (int i = 0; i < MAX_RECURSION; i++)
		{
			Vector2Int newGridPosition = previousGridClick + (extensionDirection * (i + 1));
			int indexNew = newGridPosition.x + newGridPosition.y * Level.Width;

			bool outOfBounds = newGridPosition.x < 0 || newGridPosition.x > Level.Width - 1 || newGridPosition.y < 0 || newGridPosition.y > Level.Height - 1;

			if (outOfBounds || Level.LevelData[indexNew].value > (int)SpecialCellTypes.Goal || i == MAX_RECURSION - 1)
			{
				if (i == 0) break; //If this is the first segment, dont create any sort of line

				Vector2Int priorGridPosition = previousGridClick + (extensionDirection * i);
				int indexPrior = priorGridPosition.x + priorGridPosition.y * Level.Width;

				if (priorGridPosition == Level.GoalLocation) //Line terminated in a goal
				{
					puzzleFailed = false;
					MiniGameModule.OnWinPuzzle();
				}
				if (lineLength == expectedLineLength)
				{
					Level.LevelData[indexPrior].value = (short)(SpecialCellTypes.ValidArrow + expectedLineLength - 1);
					Level.LevelData[indexPrior].isNumber = true;
					break;
				}
				Level.LevelData[indexPrior].value = (short)SpecialCellTypes.TerminatedArrow;

				break;
			}

			if (Level.LevelData[indexNew].value == (int)SpecialCellTypes.Goal) puzzleFailed = true;

			Level.LevelData[indexNew].value = (int)SpecialCellTypes.Line;
			Level.LevelData[indexNew].currentRotation = Vector2IntToDirectionShort(extensionDirection);
			Level.LevelData[indexNew].isNumber = false;

			lineLength++;
		}

		if(puzzleFailed) MiniGameModule.OnFailPuzzle(gui);

		return lineLength;
	}

	private void OnFalseMove()
	{
		if (previousGridClick == Vector2Int.left) return; //Last move was also invalid

		Level.LevelData[previousGridClick.x + previousGridClick.y*Level.Width].isTouched = false; //Deselect any select numbers if clicked out of bounds

		previousGridClick = Vector2Int.left;
	}

	private short Vector2IntToDirectionShort(Vector2Int direction)
	{
		if (direction.y != 0) return (short)(1 - direction.y);
		else return (short)Math.Sign(-direction.x);
	}

	private int GetExpectedLineLength(Vector2Int numberLocation)
	{
		int val = Level.LevelData[numberLocation.x + numberLocation.y*Level.Width].value;

		if (val == (int)SpecialCellTypes.Wall || val == (int)SpecialCellTypes.Line || val == (int)SpecialCellTypes.TerminatedArrow || val == (int)SpecialCellTypes.ExpendedArrow) return 0;

		if (val >= (int)SpecialCellTypes.ValidArrow) return val - (int)SpecialCellTypes.ValidArrow + 1;
		else return val - (int)SpecialCellTypes.Number + 1;
	}
}