using System;
using System.Collections.Generic;
using System.Linq;
using AddressableReferences;
using Mirror;
using ScriptableObjects;
using UI.Action;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using TileManagement;

namespace Items.Tool
{
	public class CrayonSprayCan : NetworkBehaviour, ICheckedInteractable<PositionalHandApply>, IClientInteractable<HandActivate>, IExaminable, IServerInventoryMove
	{
		[FormerlySerializedAs("setColour")] [SerializeField]
		private CrayonColour setCrayonColour = CrayonColour.White;

		public CrayonColour SetCrayonColour => setCrayonColour;

		[SerializeField]
		[Tooltip("If this isn't white then this colour will be used instead")]
		private Color customColour = Color.white;

		[SerializeField]
		[Min(0)]
		private float timeToDraw = 5;

		[SerializeField]
		[Min(-1)]
		private int charges = 30;

		//Have to have two lists of the same thing due to layering issues, and cannot dynamically change SO LayerTile
		//Due to late join client syncing as they wouldn't have that SO
		//These two lists need to be identical in sequence, i.e. have same tile in same index but with different layerTile
		[SerializeField]
		private GraffitiCategoriesScriptableObject graffitiListsFloor = null;

		[SerializeField]
		private GraffitiCategoriesScriptableObject graffitiListsWalls = null;

		[SerializeField]
		private bool isCan;
		public bool IsCan => isCan;

		[SerializeField]
		private AddressableAudioSource spraySound = null;

		[SerializeField]
		private ItemStorageCapacity crayonBoxCapacity = null;

		[SyncVar(hook = nameof(SyncCapState))]
		private bool capRemoved;

		private int categoryIndex = -1;
		private int index = -1;
		private OrientationEnum orientation = OrientationEnum.Up;

		public static readonly Dictionary<CrayonColour, Color> PickableColours = new Dictionary<CrayonColour,Color>
		{
			{CrayonColour.White, Color.white},
			{CrayonColour.Black, Color.black},
			{CrayonColour.Blue, Color.blue},
			{CrayonColour.Green, Color.green},
			{CrayonColour.Mime, Color.grey},
			{CrayonColour.Orange, new Color(1, 0.65f, 0)},
			{CrayonColour.Purple, new Color(0.6901961f, 0, 0.9490197f)},
			{CrayonColour.Yellow, Color.yellow},
			{CrayonColour.Red, Color.red}
		};

		private RegisterItem registerItem;
		private SpriteHandler spriteHandler;
		private ItemActionButton itemActionButton;

		#region LifeCycle

		private void Awake()
		{
			registerItem = GetComponent<RegisterItem>();
			itemActionButton = GetComponent<ItemActionButton>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
		}

		private void OnEnable()
		{
			if (itemActionButton == null) return;

			itemActionButton.ServerActionClicked += ServerToggleCap;
		}

		private void OnDisable()
		{
			if (itemActionButton == null) return;

			itemActionButton.ServerActionClicked -= ServerToggleCap;
		}

		#endregion

		#region PositionalHandApply

		//Drawing and Spraying
		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (interaction.HandObject == null) return false;

			if (interaction.HandObject != gameObject) return false;

			return true;
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			var cellPos = MatrixManager.WorldToLocalInt(interaction.WorldPositionTarget.RoundToInt(),
				registerItem.Matrix.MatrixInfo);

			//If space cannot use
			if (registerItem.Matrix.IsSpaceAt(cellPos, true))
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"Cannot use {gameObject.ExpensiveName()} on space");
				return;
			}

			if (charges <= 0)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"The {gameObject.ExpensiveName()} needs refilling");
				return;
			}

			if (isCan)
			{
				TryCan(interaction, cellPos);
				return;
			}

			TryCrayon(interaction, cellPos);
		}

		private void TryCrayon(PositionalHandApply interaction, Vector3Int cellPos)
		{
			//If crayon cannot use unless it is not blocked
			if (registerItem.Matrix.IsPassableAtOneMatrixOneTile(cellPos, true, false,
				ignoreObjects: true) == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"Cannot use {gameObject.ExpensiveName()} on that surface");
				return;
			}

			//Cant use crayons on walls
			if (registerItem.Matrix.IsWallAt(cellPos, true))
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"Cannot use {gameObject.ExpensiveName()} on a wall, try a spray can instead");
				return;
			}

			AddOverlay(interaction, cellPos);
		}

		private void TryCan(PositionalHandApply interaction, Vector3Int cellPos)
		{
			if (capRemoved == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "You need to remove the cap before spraying");
				return;
			}

			var isWall = registerItem.Matrix.IsWallAt(cellPos, true);

			//Can can only be used if it is not blocked or it is a wall
			if (isWall == false
			    && registerItem.Matrix.IsPassableAtOneMatrixOneTile(cellPos, true, false) == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"Cannot use {gameObject.ExpensiveName()} on that surface");
				return;
			}

			AddOverlay(interaction, cellPos, isWall);
		}

		private void AddOverlay(PositionalHandApply interaction, Vector3Int cellPos, bool isWall = false)
		{
			//Work out colour
			var chosenColour = GetColour();
			var chosenDirection = GetDirection();

			var tileToUse = GetTileFromIndex(isWall);

			if (tileToUse == null)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"You need to chose a type of graffiti to {(isCan ? "spray" : "draw")} first");
				return;
			}

			var graffitiAlreadyOnTile = registerItem.TileChangeManager
				.GetAllOverlayTiles(cellPos, isWall ? LayerType.Walls : LayerType.Floors, OverlayType.Cleanable)
				.Where(t => t.IsGraffiti).ToList();

			foreach (var graffiti in graffitiAlreadyOnTile)
			{
				//replace if type already on tile
				if (graffiti == tileToUse)
				{
					//Add change overlay
					ToolUtils.ServerUseToolWithActionMessages(interaction, timeToDraw,
						$"You begin to {(isCan ? "spray" : "draw")} graffiti on to the {(isWall ? "wall" : "floor")}...",
						$"{interaction.Performer.ExpensiveName()} starts to {(isCan ? "spray" : "draw")} graffiti on the {(isWall ? "wall" : "floor")}...",
						$"You {(isCan ? "spray" : "draw")} graffiti on to the {(isWall ? "wall" : "floor")}",
						$"{interaction.Performer.ExpensiveName()} {(isCan ? "sprays" : "draws")} graffiti on to the {(isWall ? "wall" : "floor")}",
						() =>
						{
							if (charges > 0 || charges == -1)
							{
								registerItem.TileChangeManager.RemoveOverlaysOfType(cellPos, isWall ? LayerType.Walls : LayerType.Floors, OverlayType.Cleanable);
								registerItem.TileChangeManager.AddOverlay(cellPos, tileToUse, chosenDirection, chosenColour);
							}

							UseAndCheckCharges(interaction);
						}
					);

					UseAndCheckCharges(interaction);

					//Should only ever be one of the overlay
					return;
				}
			}

			//Only allow 5 graffiti overlays on a tile
			if (graffitiAlreadyOnTile.Count > 5)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Adding any more graffiti would make the art messy");
				return;
			}

			//Add overlay
			ToolUtils.ServerUseToolWithActionMessages(interaction, timeToDraw,
				$"You begin to {(isCan ? "spray" : "draw")} on the {(isWall ? "wall" : "floor")}...",
				$"{interaction.Performer.ExpensiveName()} starts to {(isCan ? "spray" : "draw")} on the {(isWall ? "wall" : "floor")}...",
				$"You {(isCan ? "spray" : "draw")} on the {(isWall ? "wall" : "floor")}",
				$"{interaction.Performer.ExpensiveName()} {(isCan ? "sprays" : "draws")} on the {(isWall ? "wall" : "floor")}",
				() =>
				{
					if (charges > 0 || charges == -1)
					{
						registerItem.TileChangeManager.AddOverlay(cellPos, tileToUse, chosenDirection, chosenColour);
					}

					UseAndCheckCharges(interaction);
				}
			);
		}

		private Color GetColour()
		{
			//If custom colour set, use that instead
			if (customColour != Color.white)
			{
				return customColour;
			}

			if (SetCrayonColour == CrayonColour.UnlimitedRainbow)
			{
				//any random colour
				return new Color(Random.Range(0, 1f), Random.Range(0, 1f) , Random.Range(0, 1f));
			}

			if (SetCrayonColour == CrayonColour.NormalRainbow)
			{
				//random from set values
				return PickableColours.PickRandom().Value;
			}

			//chosen value
			return PickableColours[SetCrayonColour];
		}

		private Matrix4x4 GetDirection()
		{
			switch (orientation)
			{
				case OrientationEnum.Up:
					return Matrix4x4.identity;
				case OrientationEnum.Right:
					return Matrix4x4.TRS(Vector3.zero,  Quaternion.Euler(0f, 0f, 270f), Vector3.one);
				case OrientationEnum.Left:
					return Matrix4x4.TRS(Vector3.zero,  Quaternion.Euler(0f, 0f, 90f), Vector3.one);
				case OrientationEnum.Down:
					return Matrix4x4.TRS(Vector3.zero,  Quaternion.Euler(0f, 0f, 180f), Vector3.one);
				default:
					return Matrix4x4.identity;
			}
		}

		private void UseAndCheckCharges(PositionalHandApply interaction)
		{
			if (charges != -1)
			{
				charges--;
			}

			if (isCan)
			{
				SoundManager.PlayNetworkedAtPos(spraySound, interaction.Performer.WorldPosServer(), sourceObj: interaction.Performer);
			}

			// -1 means infinite
			if (charges > 0 || charges == -1) return;

			if (isCan)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"The {gameObject.ExpensiveName()} needs refilling!");
				return;
			}

			Chat.AddExamineMsgFromServer(interaction.Performer, $"There is no more of the {gameObject.ExpensiveName()} left!");
			_ = Despawn.ServerSingle(gameObject);
		}

		#endregion

		#region Chosing Tile

		//This is all client side only
		[Client]
		public bool Interact(HandActivate interaction)
		{
			if (isCan && capRemoved == false)
			{
				Chat.AddExamineMsgToClient("Need to remove the cap first");
				return false;
			}

			UIManager.Instance.CrayonUI.openingObject = gameObject;
			UIManager.Instance.CrayonUI.SetActive(true);
			return true;
		}

		public void SetTileFromClient(uint newCategoryIndex, uint newIndex, uint colourIndex, OrientationEnum direction)
		{
			categoryIndex = (int)newCategoryIndex;
			index = (int)newIndex;
			orientation = direction;

			if(isCan == false) return;

			if(colourIndex >= Enum.GetNames(typeof(CrayonColour)).Length) return;

			setCrayonColour = (CrayonColour)colourIndex;
		}

		//Both lists must have the same layout as this assumes indexes are the same
		private OverlayTile GetTileFromIndex(bool isWall)
		{
			if (categoryIndex == -1 || index == -1)
			{
				return null;
			}

			//If wall get wall variant of the overlay tile
			if (isWall)
			{
				if(graffitiListsWalls.GraffitiTilesCategories.Count < categoryIndex)
				{
					categoryIndex = -1;
					return null;
				}

				var wallCategory = graffitiListsWalls.GraffitiTilesCategories[(int)categoryIndex];

				if(wallCategory.GraffitiTiles.Count < index)
				{
					categoryIndex = -1;
					index = -1;
					return null;
				}

				return wallCategory.GraffitiTiles[index];
			}

			//Else get floor variant of the overlay tile
			if(graffitiListsFloor.GraffitiTilesCategories.Count < categoryIndex)
			{
				categoryIndex = -1;
				return null;
			}

			var floorCategory = graffitiListsFloor.GraffitiTilesCategories[(int)categoryIndex];

			if (floorCategory.GraffitiTiles.Count < index)
			{
				categoryIndex = -1;
				index = -1;
				return null;
			}

			return floorCategory.GraffitiTiles[index];
		}

		#endregion

		#region ToggleCap

		private void ServerToggleCap()
		{
			if (isCan == false) return;

			capRemoved = !capRemoved;

			spriteHandler.ChangeSprite(capRemoved ? 1 : 0);
		}

		[Client]
		private void SyncCapState(bool oldVar, bool newVar)
		{
			capRemoved = newVar;
		}

		#endregion

		//If new colour is added then add at the end or you'll mess up the prefabs
		//Also add to the colours dictionary at the top of this script
		public enum CrayonColour
		{
			White,
			Black,
			Blue,
			Green,
			Mime,
			Orange,
			Purple,
			Yellow,
			Red,
			NormalRainbow,
			UnlimitedRainbow
		}

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			return $"The {gameObject.ExpensiveName()} has {charges} uses left";
		}

		public void OnInventoryMoveServer(InventoryMove info)
		{
			if(info.ToSlot == null || info.ToSlot.ItemStorage == null) return;

			if(info.ToSlot.ItemStorage.ItemStorageCapacity != crayonBoxCapacity) return;

			if (isCan)
			{
				Chat.AddExamineMsgFromServer(info.FromPlayer.OrNull()?.gameObject, "Spray cans are not crayons!");
				return;
			}

			if (setCrayonColour == CrayonColour.Mime)
			{
				Chat.AddExamineMsgFromServer(info.FromPlayer.OrNull()?.gameObject, "This crayon is too sad to be contained in this box!");
				Inventory.ServerTransfer(info.ToSlot, info.FromSlot);
				return;
			}

			if (setCrayonColour == CrayonColour.NormalRainbow || setCrayonColour == CrayonColour.UnlimitedRainbow)
			{
				Chat.AddExamineMsgFromServer(info.FromPlayer.OrNull()?.gameObject, "This crayon is too powerful to be contained in this box!");
				Inventory.ServerTransfer(info.ToSlot, info.FromSlot);
			}
		}
	}
}
