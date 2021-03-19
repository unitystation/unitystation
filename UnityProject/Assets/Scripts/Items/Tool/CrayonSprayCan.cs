using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using ScriptableObjects;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Items.Tool
{
	public class CrayonSprayCan : MonoBehaviour, ICheckedInteractable<PositionalHandApply>, IClientInteractable<HandActivate>
	{
		[SerializeField]
		private Colour colour = Colour.White;

		[SerializeField]
		[Tooltip("If this isn't white then this colour will be used instead")]
		private Color customColour = Color.white;

		[SerializeField]
		[Min(0)]
		private float timeToDraw = 5;

		[SerializeField]
		private GraffitiCategoriesScriptableObject graffitiLists = null;

		private OverlayTile tileToUse = null;

		[SerializeField]
		private bool isCan;

		private bool capRemoved;

		public static readonly Dictionary<Colour, Color> PickableColours = new Dictionary<Colour,Color>
		{
			{Colour.White, Color.white},
			{Colour.Black, Color.black},
			{Colour.Blue, Color.blue},
			{Colour.Green, Color.green},
			{Colour.Mime, Color.grey},
			{Colour.Orange, new Color(1, 0.65f, 0)},
			{Colour.Purple, new Color(0.6901961f, 0, 0.9490197f)},
			{Colour.Yellow, Color.yellow},
			{Colour.Red, Color.red}
		};

		private RegisterItem registerItem;

		private void Awake()
		{
			registerItem = GetComponent<RegisterItem>();
		}

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
			//If space cannot use
			if (registerItem.Matrix.IsSpaceAt(interaction.WorldPositionTarget.RoundToInt(), true))
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"Cannot use {gameObject.ExpensiveName()} on space");
				return;
			}

			if (tileToUse == null)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"You need to chose a type of graffiti to {(isCan ? "spray" : "draw")} first");
				return;
			}

			if (isCan)
			{
				TryCan(interaction);
				return;
			}

			TryCrayon(interaction);
		}

		private void TryCrayon(PositionalHandApply interaction)
		{
			//If crayon cannot use unless it is not blocked
			if (registerItem.Matrix.IsPassableAtOneMatrixOneTile(interaction.WorldPositionTarget.RoundToInt(), true, false,
				ignoreObjects: true) == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"Cannot use {gameObject.ExpensiveName()} on blocked tile");
				return;
			}

			//Cant use crayons on walls
			if (registerItem.Matrix.IsWallAt(interaction.WorldPositionTarget.RoundToInt(), true))
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"Cannot use {gameObject.ExpensiveName()} on a wall, try a spray can instead");
				return;
			}

			AddOverlay(interaction);
		}

		private void TryCan(PositionalHandApply interaction)
		{
			var isWall = registerItem.Matrix.IsWallAt(interaction.WorldPositionTarget.RoundToInt(), true);

			//Can can only be used if it is not blocked or it is a wall
			if (isWall == false
			    && registerItem.Matrix.IsPassableAtOneMatrixOneTile(interaction.WorldPositionTarget.RoundToInt(), true, false) == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"Cannot use {gameObject.ExpensiveName()} on blocked tile");
				return;
			}

			AddOverlay(interaction, isWall);
		}

		private void AddOverlay(PositionalHandApply interaction, bool isWall = false)
		{
			//Work out colour
			Color chosenColour;

			//If custom colour set, use that instead
			if (customColour != Color.white)
			{
				chosenColour = customColour;
			}
			else
			{
				if (colour == Colour.UnlimitedRainbow)
				{
					//any random colour
					chosenColour = new Color(Random.Range(0, 1), Random.Range(0, 1) , Random.Range(0, 1));
				}
				else if (colour == Colour.NormalRainbow)
				{
					//random from set values
					chosenColour = PickableColours.PickRandom().Value;
				}
				else
				{
					//chosen value
					chosenColour = PickableColours[colour];
				}
			}

			var cellPos = MatrixManager.WorldToLocalInt(interaction.WorldPositionTarget.RoundToInt(),
				registerItem.Matrix.MatrixInfo);

			var graffitiAlreadyOnTile = registerItem.TileChangeManager
				.GetAllOverlayTiles(cellPos, LayerType.FloorEffects, TileChangeManager.OverlayType.Cleanable)
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
							registerItem.TileChangeManager.RemoveOverlaysOfName(cellPos, LayerType.FloorEffects, graffiti.OverlayName);
							registerItem.TileChangeManager.AddOverlay(cellPos, tileToUse, color: chosenColour);
						}
					);

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
					registerItem.TileChangeManager.AddOverlay(cellPos, tileToUse, color: chosenColour);
				}
			);
		}

		#endregion

		#region Chosing Tile

		//This is all client side only
		[Client]
		public bool Interact(HandActivate interaction)
		{
			UIManager.Instance.CrayonUI.SetActive(true);
			UIManager.Instance.CrayonUI.openingObject = gameObject;
			return true;
		}

		public void SetTileFromClient(uint categoryIndex, uint index, uint colourIndex)
		{
			if(graffitiLists.GraffitiTilesCategories.Count >= categoryIndex) return;

			var category = graffitiLists.GraffitiTilesCategories[(int)categoryIndex];

			if(category.GraffitiTiles.Count >= index) return;

			tileToUse = category.GraffitiTiles[(int)index];

			if(isCan == false) return;

			if(colourIndex >= Enum.GetNames(typeof(Colour)).Length) return;

			colour = (Colour)colourIndex;
		}

		#endregion

		//If new colour is added then add at the end or you'll mess up the prefabs
		//Also add to the colours dictionary at the top of this script
		public enum Colour
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
	}
}
