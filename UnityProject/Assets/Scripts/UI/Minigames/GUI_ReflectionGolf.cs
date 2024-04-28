using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MiniGames.MiniGameModules;
using UnityEngine.UI;
using NaughtyAttributes;

namespace UI.Minigames
{
	public class GUI_ReflectionGolf : NetTab
	{
		public ReflectionGolfModule miniGameModule;
		private ReflectionGolfInput gridInput;

		private List<GridCell> cells = new List<GridCell>();

		public int expectedCellCount { get; private set; } = 0;

		private bool isUpdating = false;

		[SerializeField]
		private Sprite[] possibleSprites = new Sprite[16];
		[SerializeField]
		private GameObject cellPrefab = null;

		[SerializeField]
		private Image gridImage = null;

		[SerializeField]
		private RectTransform parentTransform = null;

		private const int CELL_BASE_SIZE = 100;
		private const int MAXIMUM_ALLOWABLE_CELLS = 200; //This should be more than enough for any reasonable puzzle (can be expanded if need be) but just stops an ifinite loop from occuring if things go wrong.

		private void Start()
		{
			gridInput = new ReflectionGolfInput(parentTransform);
		}

		protected override void InitServer()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				StartCoroutine(WaitForProvider());
			}
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			miniGameModule = Provider.GetComponent<ReflectionGolfModule>();
			miniGameModule.AttachGui(this);

			UpdateGUI();
			
			OnTabOpened.AddListener(UpdateGUIForPeepers);
		}

		private void UpdateGUI() //Client side
		{
			if(gridInput.initialised == false) gridInput.AttachModule(miniGameModule);

			UpdateCellSprites();
		}

		public void OnGridPress()
		{
			gridInput.OnGridPress(Input.mousePosition);
		}

		public void UpdateGUIForPeepers(PlayerInfo notUsed)
		{
			if (!isUpdating)
			{
				isUpdating = true;
				StartCoroutine(WaitForClient());
			}
		}

		private IEnumerator WaitForClient()
		{
			yield return new WaitForSeconds(0.2f);
			UpdateGUI();

			isUpdating = false;
		}


		/// <summary>
		/// Ensures we always have the right amount of cell gameobjects, never too many or too little.
		/// </summary>
		public void TrimExtendCellList()
		{
			if (cells.Count == expectedCellCount) return;
			while (cells.Count > expectedCellCount && cells.Count > 0)
			{
				Destroy(cells[cells.Count - 1].transformComponent.gameObject);
				cells.RemoveAt(cells.Count - 1);
			}

			if (cells.Count == expectedCellCount) return;
			while (cells.Count < expectedCellCount && cells.Count < MAXIMUM_ALLOWABLE_CELLS)
			{
				var newCell = Instantiate(cellPrefab, parentTransform);
				var newGridCell = new GridCell();
				newGridCell.spriteComponenet = newCell.GetComponent<Image>();
				newGridCell.transformComponent = newCell.GetComponent<RectTransform>();

				if (newGridCell.spriteComponenet == null || newGridCell.transformComponent == null) //This should ideally never happen, but just incase stop while we are ahead.
				{
					Destroy(newCell);
					return;
				}

				cells.Add(newGridCell);
			}
		}


		/// <summary>
		/// Testing function for testing the rendering of grid cells and levels. 
		/// </summary>
		[ExecuteInEditMode, Button("TestUpdateCellSprites")]
		public void TestCells()
		{
			cells.Clear();

			ReflectionGolfModule newGame = gameObject.AddComponent(typeof(ReflectionGolfModule)) as ReflectionGolfModule;
			newGame.StartMiniGame();
			miniGameModule = newGame;


			newGame.AttachGui(this);
			UpdateCellSprites();

			Debug.Log(expectedCellCount);

			DestroyImmediate(newGame);
		}

		public void UpdateCellSprites()
		{
			int cellIndex = 0;
			
			if (cells.Count != expectedCellCount) TrimExtendCellList();

			parentTransform.sizeDelta = new Vector2(miniGameModule.ScaleFactor * miniGameModule.currentLevel.Width, miniGameModule.ScaleFactor * miniGameModule.currentLevel.Height);
			gridImage.pixelsPerUnitMultiplier = CELL_BASE_SIZE / miniGameModule.ScaleFactor;

			for (int y = 0; y < miniGameModule.currentLevel.Height; y++)
			{
				for (int x = 0; x < miniGameModule.currentLevel.Width; x++)
				{
					int unfilteredIndex = miniGameModule.currentLevel.LevelData[x, y].value;
					Debug.Log($"{x}{y}: {unfilteredIndex}");
					if (unfilteredIndex <= 0) continue;

					float rotation = (float)(miniGameModule.currentLevel.LevelData[x, y].currentRotation * Math.PI / 2);

					cells[cellIndex].transformComponent.rotation = Quaternion.Euler(0, 0, rotation);
					cells[cellIndex].transformComponent.sizeDelta = new Vector2(miniGameModule.ScaleFactor, miniGameModule.ScaleFactor);
					cells[cellIndex].transformComponent.anchoredPosition = new Vector3((x + 0.5f) * miniGameModule.ScaleFactor, (y + 0.5f) * miniGameModule.ScaleFactor, 0);

					int spriteIndex = unfilteredIndex;

					if(unfilteredIndex >= (int)SpecialCellTypes.ValidArrow)
					{
						spriteIndex = miniGameModule.currentLevel.LevelData[x, y].isNumber ? (int)SpecialCellTypes.ValidArrow : (int)SpecialCellTypes.ExpendedArrow;
					}
					cells[cellIndex].spriteComponenet.sprite = possibleSprites[spriteIndex];

					cellIndex++;
				}
			}
		}

		public void UpdateCellSpriteColour()
		{
			int cellIndex = 0;

			for (int y = 0; y < miniGameModule.currentLevel.Height; y++)
			{
				for (int x = 0; x < miniGameModule.currentLevel.Width; x++)
				{
					int unfilteredIndex = miniGameModule.currentLevel.LevelData[x, y].value;
					if (unfilteredIndex <= 0) continue;

					Color drawColor = Color.white;
					if (miniGameModule.currentLevel.LevelData[x, y].isTouched == true) drawColor = Color.grey;

					cells[cellIndex].spriteComponenet.color = drawColor;
				}
			}
		}

		public void UpdateExpectedCellCount(int newValue)
		{
			expectedCellCount = newValue;
		}

		private struct GridCell //Saves us regetting componenets every update
		{
			public RectTransform transformComponent;
			public Image spriteComponenet;
		}
	}
}
