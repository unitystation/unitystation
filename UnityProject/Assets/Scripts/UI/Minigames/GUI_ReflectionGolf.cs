using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiniGames.MiniGameModules;
using UnityEngine.UI;
using Logs;

namespace UI.Minigames
{
	public class GUI_ReflectionGolf : NetTab
	{
		public ReflectionGolfModule MiniGameModule { get; private set; } = null;
		private ReflectionGolfInput gridInput;

		private readonly List<GridCell> cells = new List<GridCell>();

		private bool isUpdating = false;

		[SerializeField]
		private Sprite[] possibleSprites = new Sprite[16];

		[SerializeField]
		private Image[] UndoLights = new Image[3];

		[SerializeField]
		private Sprite[] lightSprites = new Sprite[2];

		[SerializeField]
		private Sprite[] completeLightSprites = new Sprite[3];

		[SerializeField]
		private Image gridImage = null;

		[SerializeField]
		private Image puzzleCompleteImage = null;

		[SerializeField]
		private RectTransform parentTransform = null;

		private RectTransform guiTransform = null;

		[SerializeField]
		private GameObject cellPrefab = null;

		private const int CELL_BASE_SIZE = 100;
		private const int MAXIMUM_ALLOWABLE_CELLS = 300;


		#region Lifecycle

		private void Start()
		{
			guiTransform = this.GetComponent<RectTransform>();
			gridInput = new ReflectionGolfInput(parentTransform);
			puzzleCompleteImage.sprite = completeLightSprites[0];
		}

		public void Awake()
		{
			StartCoroutine(WaitForProvider());		
		}

		public override void OnEnable()
		{
			hasBeenClosed = false;
			UpdateGui();

			base.OnEnable();
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			MiniGameModule = Provider.GetComponent<ReflectionGolfModule>();
			MiniGameModule.OnGuiUpdate += UpdateGui;
			MiniGameModule.GetTracker().OnGameWon.AddListener(OnWin);

			UpdateGui();

			if (CustomNetworkManager.IsServer == false) OnTabOpened.AddListener(UpdateGuiForPeepers);
		}	

		public void UpdateGui() //Client side
		{
			if(gridInput.Initialised == false) gridInput.AttachGui(this);

			int lightCount = MiniGameModule.FetchUndoCount();
			foreach(var light in UndoLights)
			{
				light.sprite = lightSprites[lightCount > 0 ? 1 : 0];
				lightCount--;
			}

			UpdateCellSprites();
		}

		public void UpdateGuiForPeepers(PlayerInfo notUsed)
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
			UpdateGui();

			isUpdating = false;
		}

		public void OnUndoButtonPress()
		{
			if (gridInput != null) gridInput.OnUndo();
		}

		public void OnRestartButtonPress()
		{
			if (MiniGameModule == null) return;
			if (CustomNetworkManager.IsServer == false) MiniGameModule.CmdReloadLevel();
			else MiniGameModule.RestartLevel();	
		}

		#endregion

		#region GRID

		public void OnGridPress()
		{
			gridInput.OnGridPress(Input.mousePosition, guiTransform.position);
		}

		/// <summary>
		/// Ensures we always have the right amount of cell gameobjects, never too many or too little.
		/// </summary>
		public void TrimExtendCellList()
		{
			int expectedCellCount = MiniGameModule.ExpectedCellCount;

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

		public void UpdateCellSprites()
		{
			int cellIndex = 0;
			
			if (cells.Count != MiniGameModule.ExpectedCellCount) TrimExtendCellList();

			parentTransform.sizeDelta = new Vector2(MiniGameModule.ScaleFactor * MiniGameModule.CurrentLevel.Width, MiniGameModule.ScaleFactor * MiniGameModule.CurrentLevel.Height);
			gridImage.pixelsPerUnitMultiplier = CELL_BASE_SIZE / MiniGameModule.ScaleFactor;

			for (int y = 0; y < MiniGameModule.CurrentLevel.Height; y++)
			{
				for (int x = 0; x < MiniGameModule.CurrentLevel.Width; x++)
				{
					int unfilteredIndex = MiniGameModule.CurrentLevel.LevelData[x + y*MiniGameModule.CurrentLevel.Width].value;

					if (unfilteredIndex <= 0) continue;

					float rotation = MiniGameModule.CurrentLevel.LevelData[x + y * MiniGameModule.CurrentLevel.Width].currentRotation * 90;

					cells[cellIndex].transformComponent.rotation = Quaternion.Euler(0, 0, rotation);
					cells[cellIndex].transformComponent.sizeDelta = new Vector2(MiniGameModule.ScaleFactor, MiniGameModule.ScaleFactor);
					cells[cellIndex].transformComponent.anchoredPosition = new Vector3((x + 0.5f) * MiniGameModule.ScaleFactor, (y + 0.5f) * MiniGameModule.ScaleFactor, 0);

					int spriteIndex = unfilteredIndex;

					if(unfilteredIndex >= (int)SpecialCellTypes.ValidArrow)
					{
						spriteIndex = MiniGameModule.CurrentLevel.LevelData[x + y * MiniGameModule.CurrentLevel.Width].isNumber ? (int)SpecialCellTypes.ValidArrow : (int)SpecialCellTypes.ExpendedArrow;
					}
					cells[cellIndex].spriteComponenet.sprite = possibleSprites[spriteIndex];

					cellIndex++;
				}
			}

			while(cellIndex <= cells.Count - 1) //The only situation this will occur is when a line/arrow has overridden the goal, in this situation we dont want to destroy this excess cell as the goal still exists, but we need to hide it.
			{
				cells[cellIndex].transformComponent.anchoredPosition = new Vector2(-MiniGameModule.ScaleFactor, -MiniGameModule.ScaleFactor);
				cellIndex++;
			}
		}

		public void UpdateCellSpriteColour()
		{
			int cellIndex = 0;

			if (cells.Count != MiniGameModule.ExpectedCellCount) TrimExtendCellList();

			for (int y = 0; y < MiniGameModule.CurrentLevel.Height; y++)
			{
				for (int x = 0; x < MiniGameModule.CurrentLevel.Width; x++)
				{
					int unfilteredIndex = MiniGameModule.CurrentLevel.LevelData[x + y * MiniGameModule.CurrentLevel.Width].value;
					if (unfilteredIndex <= 0) continue;

					Color drawColor = Color.white;
					if (MiniGameModule.CurrentLevel.LevelData[x + y * MiniGameModule.CurrentLevel.Width].isTouched == true) drawColor = Color.grey;

					cells[cellIndex].spriteComponenet.color = drawColor;

					cellIndex++;
				}
			}
		}

		#endregion

		#region OnWinLose

		public void OnWin()
		{
			puzzleCompleteImage.sprite = completeLightSprites[1];

			StartCoroutine(WaitToClose());

			MiniGameModule.GetTracker().OnGameWon.RemoveListener(OnWin);
		}

		public void OnFail()
		{
			puzzleCompleteImage.sprite = completeLightSprites[0];
		}

		private IEnumerator WaitToClose()
		{
			yield return new WaitForSeconds(3);
			OnCloseTab();
		}

		bool hasBeenClosed = false;
		public void OnCloseTab()  //Making sure the coroutine doesnt try close an already closed tab
		{
			if (hasBeenClosed == true) return;
			hasBeenClosed = true;

			CloseTab();
		}

		#endregion

	}

	public struct GridCell //Saves us regetting componenets every update
	{
		public RectTransform transformComponent;
		public Image spriteComponenet;
	}
}
