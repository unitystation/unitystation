using MiniGames.MiniGameModules;
using UnityEngine;
using System.Drawing;
using System;

public class ReflectionGolfInput
{
	private ReflectionGolfModule miniGameModule = null;
	private ReflectionGolfLevel level => miniGameModule.currentLevel;
	private float cellSize => miniGameModule.ScaleFactor;

	private RectTransform gridTransform = null;

	private Vector2Int previousGridClick = Vector2Int.zero;

	private bool isGameActive => miniGameModule.miniGameActive;

	private int MAX_RECURSION = 20; //This can be increased if needed, just here to prevent inifinite loops in situtation wehere things go wrong.

	public bool initialised { get; private set; } = false;

	public ReflectionGolfInput(RectTransform transform)
	{
		gridTransform = transform;
	}

	public void AttachModule(ReflectionGolfModule module)
	{
		miniGameModule = module;
		initialised = true;
	}

	public void OnGridPress(Vector3 mousePosition)
	{
		if (initialised == false || isGameActive == false) return;

		float _cellSize = cellSize;

		Vector2 pos = (mousePosition - gridTransform.position).To2(); //To local coordinate space

		if(level.Width < level.Height) pos.x = pos.x - ((level.Height - level.Width) * _cellSize / 2);
		else if(level.Height < level.Width) pos.y = pos.y - ((level.Width - level.Height) * _cellSize / 2);

		Vector2Int clickedGridPosition = new Vector2Int((int)(pos.x / _cellSize), (int)(pos.y / _cellSize));

		if (clickedGridPosition.x < 0 || clickedGridPosition.x > level.Width - 1 || clickedGridPosition.y < 0 || clickedGridPosition.y < level.Height - 1)
		{
			OnFalseMove();
			return;
		}

		if (level.LevelData[clickedGridPosition.x, clickedGridPosition.y].isNumber == true) OnNumberClick(clickedGridPosition);
		else OnNonNumberClick(clickedGridPosition);
	}

	private void OnNumberClick(Vector2Int clickPosition) //If we click on a number, select that number, and if the previous click was also a number, deselect it.
	{
		if(previousGridClick != Vector2Int.left)
		{
			CellData oldCellData = level.LevelData[previousGridClick.x, previousGridClick.y];

			if (oldCellData.isTouched == true && oldCellData.isNumber == true) oldCellData.isTouched = false;
			level.LevelData[previousGridClick.x, previousGridClick.y] = oldCellData;
		}

		CellData newCellData = level.LevelData[clickPosition.x, clickPosition.y];
		newCellData.isTouched = !newCellData.isTouched;

		level.LevelData[clickPosition.x, clickPosition.y] = newCellData;

		previousGridClick = clickPosition;
	}

	private void OnNonNumberClick(Vector2Int clickPosition)
	{
		if (previousGridClick == Vector2Int.left)
		{
			previousGridClick = clickPosition;
			return;
		}
		
		CellData oldCellData = level.LevelData[previousGridClick.x, previousGridClick.y];

		if (oldCellData.isNumber == false || oldCellData.isTouched == false)
		{
			previousGridClick = clickPosition;
			return; //Unless we enabled a number with our last click, do nothing
		}

		oldCellData.isNumber = false;
		oldCellData.isTouched = false; //Disable a number after it has moved

		level.LevelData[previousGridClick.x, previousGridClick.y] = oldCellData;
		
		Vector2Int extensionDirection;

		if (Math.Abs(clickPosition.x - previousGridClick.x) > Math.Abs(clickPosition.y - previousGridClick.y)) extensionDirection = new Vector2Int(Math.Sign(clickPosition.x - previousGridClick.x), 0);
		else extensionDirection = new Vector2Int(0, Math.Sign(clickPosition.y - previousGridClick.y));

		int lineLength = ExtendNumber(extensionDirection);

		if (lineLength != 0)
		{
			oldCellData.isNumber = false;
			oldCellData.isTouched = false; //Disable a number after it has moved
		}

		level.LevelData[previousGridClick.x, previousGridClick.y] = oldCellData;

		miniGameModule.InsertNewMove(previousGridClick, clickPosition);
	}

	private int ExtendNumber(Vector2Int extensionDirection)
	{
		int lineLength = 0;
		int expectedLineLength = GetExpectedLineLength(previousGridClick);

		for (int i = 0; i < MAX_RECURSION; i++)
		{
			Vector2Int newGridPosition = previousGridClick + (extensionDirection * (i + 1));

			bool outOfBounds = newGridPosition.x < 0 || newGridPosition.x > level.Width - 1 || newGridPosition.y < 0 || newGridPosition.y < level.Height - 1;

			if (outOfBounds || level.LevelData[newGridPosition.x, newGridPosition.y].value > (int)SpecialCellTypes.Goal || i == MAX_RECURSION - 1)
			{
				if (i == 0) break; //If this is the first segment, dont create any sort of line

				Vector2Int priorGridPosition = previousGridClick + (extensionDirection * i);

				if (lineLength == expectedLineLength)
				{
					level.LevelData[priorGridPosition.x, priorGridPosition.y].value = (short)(SpecialCellTypes.ValidArrow + expectedLineLength - 1);
					level.LevelData[priorGridPosition.x, priorGridPosition.y].isNumber = true;
					break;
				}
				level.LevelData[priorGridPosition.x, priorGridPosition.y].value = (short)SpecialCellTypes.TerminatedArrow;

				break;
			}

			level.LevelData[newGridPosition.x, newGridPosition.y].value = (int)SpecialCellTypes.Line;
			level.LevelData[newGridPosition.x, newGridPosition.y].currentRotation = Vector2IntToDirectionShort(extensionDirection);
			level.LevelData[newGridPosition.x, newGridPosition.y].isNumber = false;

			lineLength++;
		}

		return lineLength;
	}

	private void OnFalseMove()
	{
		if (previousGridClick == Vector2Int.left) return; //Last move was also invalid

		level.LevelData[previousGridClick.x, previousGridClick.y].isTouched = false; //Deselect any select numbers if clicked out of bounds

		previousGridClick = Vector2Int.left;
	}

	private short Vector2IntToDirectionShort(Vector2Int direction)
	{
		return (short)(((direction.y + 1) * 2) + direction.x);
	}

	private int GetExpectedLineLength(Vector2Int numberLocation)
	{
		int val = level.LevelData[numberLocation.x, numberLocation.y].value;

		if (val == (int)SpecialCellTypes.Wall || val == (int)SpecialCellTypes.Line || val == (int)SpecialCellTypes.TerminatedArrow || val == (int)SpecialCellTypes.ExpendedArrow) return 0;

		if (val >= (int)SpecialCellTypes.ValidArrow) return val - (int)SpecialCellTypes.ValidArrow + 1;
		else return val - (int)SpecialCellTypes.Number + 1;
	}
}