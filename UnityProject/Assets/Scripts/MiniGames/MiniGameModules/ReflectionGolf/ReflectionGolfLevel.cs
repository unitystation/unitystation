using MiniGames.MiniGameModules;
using System;
using UnityEngine;
using System.Text.RegularExpressions;
using System.IO;

namespace MiniGames.MiniGameModules
{
	public class ReflectionGolfLevel
	{

		public const int DISPLAY_WIDTH = 600;

		public short Width { get; private set; }

		public short Height { get; private set; }

		public CellData[,] LevelData { get; private set; } = new CellData[0, 0]; //Must be synced

		private string name = ""; //Only needed server side

		public Vector2Int GoalLocation { get; private set; }

		private ReflectionGolfModule miniGameModule = null;

		public ReflectionGolfLevel(string fileName, ReflectionGolfModule _miniGameModule)
		{
			miniGameModule = _miniGameModule;
			Initialise(fileName);
		}

		public ReflectionGolfLevel(CellData[,] levelData, ReflectionGolfModule _miniGameModule)
		{
			miniGameModule = _miniGameModule;
			LevelData = levelData;
			Width = (short)levelData.GetLength(0);
			Height = (short)levelData.GetLength(1);

			FindGoal();
		}

		public void Initialise(string fileName)
		{
			name = fileName;
			LoadLevelFromFile(fileName);
		}

		public void Reload()
		{
			LoadLevelFromFile(name);
		}


		public void LoadLevelFromFile(string fileName)
		{
			miniGameModule.ClearAllMoves();

			//if (name == "") return;
			/*string level = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Content", "Puzzles", fileName));

			MatchCollection matches = Regex.Matches(level, @"([-\d ]+)+");

			Height = (short)matches.Count;


			LevelData = new CellData[Width, Height];



			for(int y = 0; y < Height; y++)
			{
				string[] Values = matches[y].Value.Split(' ');

				for (int x = 0; x < Width; x++)
				{
					LevelData[x, y].value = Int16.Parse(Values[x]);
					uniqueObjects += PlaceObject(LevelData[x, y].value, x, y);
				}
			}*/

			int uniqueObjects = 0;

			Height = 8;
			Width = 8;

			LevelData = new CellData[Width, Height];

			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					var newCell = new CellData();
					newCell.value = 0;
					newCell.currentRotation = 0;
					newCell.isTouched = false;

					if (x == 0 || x == Width - 1) newCell.value = 2;
					else if (x == 1 && y == 1) newCell.value = 1;
					else if (y == 0 && x % 2 == 1 && x != 9) newCell.value = (short)(2 + x);
					LevelData[x, y] = newCell;

					uniqueObjects += PlaceObject(LevelData[x, y].value, x, y);
				}
			}

			miniGameModule.UpdateCellsData(uniqueObjects);
		}

		public void UpdateCell(int x, int y, CellData newData)
		{
			LevelData[x, y] = newData;
		}

		private short PlaceObject(int index, int x, int y)
		{
			switch (index)
			{
				case < 1:
					return 0;
				case 1:
					GoalLocation = new Vector2Int(x, y);
					return 1;
				default:
					return 1;
			}
		}

		private void FindGoal()
		{
			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					if(LevelData[x, y].value != (int)SpecialCellTypes.Goal) continue;

					GoalLocation = new Vector2Int(x, y);
					return;
				}
			}
		}

	}

	public struct CellData
	{
		public short value;
		public short currentRotation;
		public bool isTouched;
		public bool isNumber;
	}
}
