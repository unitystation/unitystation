using System;
using UnityEngine;
using System.Text.RegularExpressions;
using System.IO;
using Logs;
using SecureStuff;

namespace MiniGames.MiniGameModules
{
	public class ReflectionGolfLevel
	{

		public const int DISPLAY_WIDTH = 600;

		public short Width { get; private set; }

		public short Height { get; private set; }

		public CellData[] LevelData { get; private set; } = new CellData[0]; //Must be synced

		private string name = ""; //Only needed server side

		public Vector2Int GoalLocation { get; private set; }

		private ReflectionGolfModule miniGameModule = null;

		public ReflectionGolfLevel(string fileName, ReflectionGolfModule _miniGameModule)
		{
			miniGameModule = _miniGameModule;
			name = fileName;
			LoadLevelFromFile(fileName);
		}

		public ReflectionGolfLevel(CellData[] levelData, short width, ReflectionGolfModule _miniGameModule)
		{
			miniGameModule = _miniGameModule;
			LevelData = levelData;
			Width = width;
			Height = (short)(levelData.Length / width);

			InitialiseLevelValues();

			miniGameModule.miniGameGUI.UpdateGUI();
		}

		public void LoadLevelFromFile(string fileName)
		{
			miniGameModule.ClearAllMoves();

			if (name == "") return;
			string path = Path.Combine("MiniGamesData", "ReflectionGolf", fileName + ".txt");

			if (AccessFile.Exists(path) == false)
			{
				Loggy.LogError($"MiniGames/ReflectionGolfLevel.cs at line 64. The specified file path does not exist! {path}");
				return;
			}
			string level = AccessFile.Load(path);

			MatchCollection matches = Regex.Matches(level, @"([-\d ]+)+");

			Height = (short)matches.Count;

			int uniqueObjects = 0;
			for (int y = 0; y < Height; y++)
			{
				string[] Values = matches[y].Value.Split(' ');

				if(y == 0)
				{
					Width = (short)Values.Length;
					LevelData = new CellData[Width * Height];
					LevelData[0].width = Width;
				}

				for (int x = 0; x < Width; x++)
				{
					var newCell = new CellData();
					newCell.value = 0;
					newCell.currentRotation = 0;
					newCell.isTouched = false;

					newCell.value = Int16.Parse(Values[x]);
					if (newCell.value != 0) uniqueObjects++;
					if (newCell.value == (int)SpecialCellTypes.Goal) GoalLocation = new Vector2Int(x, y);
					if (newCell.value >= (int)SpecialCellTypes.Number) newCell.isNumber = true;

					LevelData[x + y*Width] = newCell;
				}
			}
			miniGameModule.UpdateCellsData(uniqueObjects);
		}

		public void UpdateCell(int x, int y, CellData newData)
		{
			LevelData[x + y*Width] = newData;
		}
	

		private void InitialiseLevelValues()
		{
			int uniqueObjects = 0;

			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					if (LevelData[x + y*Width].value != (int)SpecialCellTypes.None) uniqueObjects++;
					if(LevelData[x + y*Width].value != (int)SpecialCellTypes.Goal) continue;

					GoalLocation = new Vector2Int(x, y);
				}
			}

			miniGameModule.UpdateCellsData(uniqueObjects);
		}

	}

	public struct CellData
	{
		public short value;
		public short currentRotation;
		public bool isTouched;
		public bool isNumber;
		public short width;
	}
}
