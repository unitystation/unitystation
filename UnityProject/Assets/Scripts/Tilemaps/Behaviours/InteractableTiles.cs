using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using TileManagement;
using Mirror;
using HealthV2;
using Logs;
using Messages.Client;
using Messages.Client.Interaction;
using Systems.Electricity;
using Tiles;


/// <summary>
/// Main entry point for Tile Interaction system.
/// Component which allows all the tiles of a matrix to be interacted with using their configured
/// TileInteractions.
///
/// Also provides various utility methods for working with tiles.
/// </summary>
public class InteractableTiles : MonoBehaviour, IClientInteractable<PositionalHandApply>, IExaminable, IClientInteractable<MouseDrop>
{
	private MetaTileMap metaTileMap;
	private Matrix matrix;
	private TileChangeManager tileChangeManager;

	/// <summary>
	/// MetaTileMap that can be interacted with
	/// </summary>
	public MetaTileMap MetaTileMap => metaTileMap;

	/// <summary>
	/// Matrix that can be interacted with
	/// </summary>
	public Matrix Matrix => matrix;

	/// <summary>
	/// Tile change manager that can be interacted with
	/// </summary>
	public TileChangeManager TileChangeManager => tileChangeManager;

	private Layer floorLayer;
	public Layer FloorLayer => floorLayer;
	private Layer baseLayer;
	public Layer BaseLayer => baseLayer;
	private Layer wallLayer;
	public Layer WallLayer => wallLayer;
	private Layer windowLayer;
	public Layer WindowLayer => windowLayer;
	private ObjectLayer objectLayer;
	public ObjectLayer ObjectLayer => objectLayer;
	private Layer underFloorLayer;
	public Layer UnderFloorLayer => underFloorLayer;

	private Layer grillTileMap;

	// source: ElectricalCableDeconstruction.cs
	[Tooltip("Action message to performer when they begin cable cutting interaction.")]
	[SerializeField]
	private string performerStartActionMessage = null;

	// source: ElectricalCableDeconstruction.cs
	[Tooltip("Use {performer} for performer name. Action message to others when the performer begins cable cutting interaction.")]
	[SerializeField]
	private string othersStartActionMessage = null;

	private static readonly List<LayerType> UnderFloorsLayers = new List<LayerType>
	{
		LayerType.Underfloor, LayerType.Electrical, LayerType.Pipe, LayerType.Disposals
	};

	private void Start()
	{
		metaTileMap = GetComponentInChildren<MetaTileMap>();
		matrix = GetComponentInChildren<Matrix>();
		objectLayer = GetComponentInChildren<ObjectLayer>();
		tileChangeManager = GetComponent<TileChangeManager>();
		CacheTileMaps();
	}

	/// <summary>
	/// Gets the interactable tiles for the matrix at the indicated world position. Never returns null - always
	/// returns the interactable tiles for the appropriate matrix.
	/// </summary>
	/// <param name="worldPos"></param>
	/// <returns></returns>
	public static InteractableTiles GetAt(Vector2 worldPos, bool isServer)
	{
		var matrixInfo = MatrixManager.AtPoint(worldPos.RoundToInt(), isServer);
		return matrixInfo.TileChangeManager.InteractableTiles;
	}

	/// <summary>
	/// Gets the interactable tiles for the matrix at the indicated world position. Unless there's only space!
	/// in that case tries to fetch an adjacent matrix, in the case of none, it returns the space matrix
	/// </summary>
	public static Matrix TryGetNonSpaceMatrix(Vector3Int worldPos, bool isServer)
	{
		var matrixInfo = MatrixManager.AtPoint(worldPos, isServer);
		if (matrixInfo.Matrix.IsSpaceMatrix == false)
		{
			return matrixInfo.Matrix;
		}

		//This is just space! Lets try getting an adjacent matrix
		foreach (var pos in worldPos.BoundsAround().allPositionsWithin)
		{
			matrixInfo = MatrixManager.AtPoint(pos, isServer);
			if (matrixInfo.Matrix.IsSpaceMatrix == false)
			{
				return matrixInfo.Matrix;
			}
		}

		//we're in space and theres nothing but space all around us, we tried.
		return MatrixManager.Instance.spaceMatrix;
	}

	/// <summary>
	/// Gets the interactable tiles for the matrix at the indicated world position. Never returns null - always
	/// returns the interactable tiles for the appropriate matrix.
	/// </summary>
	/// <param name="worldPos"></param>
	/// <returns></returns>
	public static InteractableTiles GetAt(Vector2 worldPos, NetworkSide side)
	{
		return GetAt(worldPos, side == NetworkSide.Server);
	}

	/// <summary>
	/// Gets the LayerTile of the tile at the indicated position, null if no tile there (open space).
	/// </summary>
	/// <param name="worldPos"></param>
	/// <returns></returns>
	public LayerTile LayerTileAt(Vector2 worldPos, bool ignoreEffectsLayer = false, bool excludeNonIntractable = false)
	{
		Vector3Int pos = objectLayer.transform.InverseTransformPoint(worldPos).RoundToInt();

		return metaTileMap.GetTile(pos, ignoreEffectsLayer, excludeNonIntractable: excludeNonIntractable);
	}

	public LayerTile InteractableLayerTileAt(Vector2 worldPos, bool ignoreEffectsLayer = false)
	{
		return LayerTileAt(worldPos, ignoreEffectsLayer, true);
	}


	/// <summary>
	/// Gets the LayerTile of the tile at the indicated position, null if no tile there (open space).
	/// </summary>
	/// <param name="worldPos"></param>
	/// <returns></returns>
	public LayerTile LayerTileAt(Vector2 worldPos, LayerTypeSelection ExcludedLayers)
	{
		Vector3Int pos = objectLayer.transform.InverseTransformPoint(worldPos).RoundToInt();

		return metaTileMap.GetTile(pos, ExcludedLayers);
	}

	/// <summary>
	/// Converts the world position to a cell position on these tiles.
	/// </summary>
	/// <param name="worldPosition"></param>
	/// <returns></returns>
	public Vector3Int WorldToCell(Vector2 worldPosition)
	{
		return baseLayer.WorldToCell(worldPosition);
	}

	void CacheTileMaps()
	{
		var tilemaps = GetComponentsInChildren<Layer>(true);
		for (int i = 0; i < tilemaps.Length; i++)
		{
			if (tilemaps[i].name.Contains("Floors"))
			{
				floorLayer = tilemaps[i];
			}

			if (tilemaps[i].name.Contains("Base"))
			{
				baseLayer = tilemaps[i];
			}

			if (tilemaps[i].name.Contains("Walls"))
			{
				wallLayer = tilemaps[i];
			}

			if (tilemaps[i].name.Contains("Windows"))
			{
				windowLayer = tilemaps[i];
			}

			if (tilemaps[i].name.Contains("Grills"))
			{
				grillTileMap = tilemaps[i];
			}

			if (tilemaps[i].name.Contains("UnderFloor"))
			{
				underFloorLayer = tilemaps[i];
			}
		}
	}

	public string Examine(Vector3 pos)
	{
		// Get Tile at position
		LayerTile tile = LayerTileAt(pos);

		if (tile == null)
		{
			return "Space";
		}

		var desc = (tile.Description ?? string.Empty).Trim();
		StringBuilder sb = new($"This is a {tile.DisplayName}. {desc}");
		if (desc != string.Empty && !desc.EndsWith('.') && !desc.EndsWith('!') && !desc.EndsWith('?'))
		{
			sb.Append('.');
		}

		if (tile is IExaminable examinable)
		{
			sb.AppendLine(examinable.Examine());
		}

		return sb.ToString();
	}

	/// <summary>
	/// Interaction callback to attempt to interact with tiles using a HandApply
	/// interaction. We check for both basic tiles and under floor tiles and
	/// attempt to apply its interaction. In the event that an electric cable
	/// tile is encountered, we open the cable cutting window.
	/// </summary>
	/// <param name="interaction">Position of interaction with a hand</param>
	public bool Interact(PositionalHandApply interaction)
	{
		// translate to the tile interaction system
		Vector3Int localPosition = WorldToCell(interaction.WorldPositionTarget);
		// pass the interaction down to the basic tile
		LayerTile tile = InteractableLayerTileAt(interaction.WorldPositionTarget, true);

		// If the tile we're looking at is a basic tile...
		if (tile is BasicTile basicTile)
		{
			foreach (var layerType in UnderFloorsLayers)
			{
				if(tile.LayerType != layerType) continue;

				// If the the tile is something that's supposed to be underneath floors...
				if (FindLayerInteraction(interaction, localPosition, layerType, interaction.PerformerMind)) return true;
				break;
			}

			var tileApply = new TileApply(interaction.Performer, interaction.UsedObject, interaction.Intent, interaction.PerformerMind,
				(Vector2Int) localPosition, this, basicTile, interaction.HandSlot, interaction.TargetPosition);

			return TryInteractWithTile(tileApply);
		}

		return false;
	}

	private bool FindLayerInteraction(PositionalHandApply interaction, Vector3Int localPosition, LayerType layer, Mind inMind)
	{
		// Then we loop through each under floor layer in the matrix until we
		// can find an interaction.
		foreach (BasicTile underFloorTile in matrix.MetaTileMap.GetAllTilesByType<BasicTile>(localPosition,
			         layer))
		{
			// If pointing at electrical cable tile and player is holding
			// Wirecutter in hand, we enable the cutting window and return false
			// to indicate that we will not be interacting with anything... yet.
			// TODO: Check how many cables we have first. Only open the cable
			//       cutting window when the number of cables exceeds 2.
			if (underFloorTile is ElectricalCableTile &&
			    Validations.HasItemTrait(PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot()?.ItemObject,
				    CommonTraits.Instance.Wirecutter))
			{
				// open cable cutting ui window instead of cutting cable
				EnableCableCuttingWindow();

				// return false to not cut the cable
				return false;
			}
			// Else, we attempt to interact with the tile with whatever is in our
			// at the target.
			else
			{
				var underFloorApply = new TileApply(interaction.Performer, interaction.UsedObject, interaction.Intent, interaction.PerformerMind,
					(Vector2Int)localPosition, this, underFloorTile, interaction.HandSlot, interaction.TargetPosition);

				if (TryInteractWithTile(underFloorApply))
				{
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Instantiate/enable cable cutting window
	/// </summary>
	private void EnableCableCuttingWindow()
	{
		// if LoadCableCuttingWindow script is already added, just enable window
		if (TryGetComponent(out LoadCableCuttingWindow cableCuttingWindow))
		{
			cableCuttingWindow.OpenCableCuttingWindow();
		}
		// else add component, init and enable window
		else
		{
			LoadCableCuttingWindow window = (LoadCableCuttingWindow)gameObject.AddComponent(typeof(LoadCableCuttingWindow));
			window.OpenCableCuttingWindow();
		}
	}

	/// <summary>
	/// [Message Handler] Perform cable cutting interaction on server side
	/// </summary>
	public static void ServerPerformCableCuttingInteraction(NetworkConnection conn, RequestCableCut.NetMessage message, GameObject performer)
	{


		// get object at target position
		GameObject hit = MouseUtils.GetOrderedObjectsAtPoint(message.targetWorldPosition).FirstOrDefault();
		// get matrix
		Matrix matrix = hit.GetComponentInChildren<Matrix>();

		// return if matrix is null
		if (matrix == null) return;

		// convert world position to cell position and set Z value to Z value from message
		Vector3Int targetCellPosition = matrix.MetaTileMap.WorldToCell(message.targetWorldPosition);

		// get electical tile from targetCellPosition
		ElectricalCableTile electricalCable = TileManager.GetTile(message.TileType, message.Name) as ElectricalCableTile;

		if (electricalCable == null) return;

		// add messages to chat
		// TODO readd string othersMessage = Chat.ReplacePerformer(othersStartActionMessage, message.performer);
		// TODO readd Chat.AddActionMsgToChat(message.performer, performerStartActionMessage, othersMessage);

		// source: ElectricalCableDeconstruction.cs
		var metaDataNode = matrix.GetMetaDataNode(targetCellPosition);
		foreach (var ElectricalData in metaDataNode.ElectricalData)
		{
			if (ElectricalData.RelatedTile != electricalCable) continue;

			// Electrocute the performer. If shock is painful enough, cancel the interaction.
			ElectricityFunctions.WorkOutActualNumbers(ElectricalData.InData);
			float voltage = ElectricalData.InData.Data.ActualVoltage;
			var electrocution = new Electrocution(voltage, message.targetWorldPosition, "cable");
			var performerLHB = performer.GetComponent<LivingHealthMasterBase>();
			var severity = performerLHB.Electrocute(electrocution);
			if (severity > LivingShockResponse.Mild) return;

			ElectricalData.InData.DestroyThisPlease();
			Spawn.ServerPrefab(electricalCable.SpawnOnDeconstruct, message.targetWorldPosition,
				count: electricalCable.SpawnAmountOnDeconstruct);

			return;
		}

	}

	private bool TryInteractWithTile(TileApply interaction)
	{
		// Iterate over the interactions for the given tile until a valid one is found.
		foreach (var tileInteraction in interaction.BasicTile.TileInteractions)
		{
			if (tileInteraction == null) continue;
			if (tileInteraction.WillInteract(interaction, NetworkSide.Client) &&
				Cooldowns.TryStartClient(interaction, CommonCooldowns.Instance.Interaction))
			{
				//request the tile interaction with this index
				RequestInteractMessage.SendTileApply(interaction, this, tileInteraction);
				return true;
			}
		}

		return false;
	}

	//for internal IF2 usages only, does server side logic for processing tileapply
	public void ServerProcessInteraction(GameObject performer, Vector2 TargetPosition,  GameObject processorObj,
			ItemSlot usedSlot, GameObject usedObject, Intent intent, Mind inMind, TileApply.ApplyType applyType)
	{
		//find the indicated tile interaction
		var worldPosTarget = (Vector2)TargetPosition.To3().ToWorld(performer.RegisterTile().Matrix);
		Vector3Int localPosition = WorldToCell(worldPosTarget);
		//pass the interaction down to the basic tile
		LayerTile tile = LayerTileAt(worldPosTarget, true);
		if (tile is BasicTile basicTile)
		{
			// check which tile interaction occurs in the correct order
			Loggy.LogTraceFormat(
					"Server checking which tile interaction to trigger for TileApply on tile {0} at worldPos {1}",
					Category.Interaction, tile.name, worldPosTarget);

			switch (basicTile.LayerType)
			{
				case LayerType.Underfloor:
					TryLayerInteraction(performer, TargetPosition, usedSlot, usedObject, intent, inMind, applyType, localPosition,
						LayerType.Underfloor);
					break;
				case LayerType.Electrical:
					TryLayerInteraction(performer, TargetPosition, usedSlot, usedObject, intent, inMind, applyType, localPosition,
						LayerType.Electrical);
					break;
				case LayerType.Pipe:
					TryLayerInteraction(performer, TargetPosition, usedSlot, usedObject, intent, inMind, applyType, localPosition,
						LayerType.Pipe);
					break;
				case LayerType.Disposals:
					TryLayerInteraction(performer, TargetPosition, usedSlot, usedObject, intent, inMind, applyType, localPosition,
						LayerType.Disposals);
					break;
				default:
				{
					var tileApply = new TileApply(performer, usedObject, intent, inMind, (Vector2Int) localPosition,
						this, basicTile, usedSlot, TargetPosition, applyType);

					PerformTileInteract(tileApply);
					break;
				}
			}
		}
	}

	private void TryLayerInteraction(GameObject performer, Vector2 TargetPosition, ItemSlot usedSlot, GameObject usedObject,
		Intent intent, Mind inMind, TileApply.ApplyType applyType, Vector3Int localPosition, LayerType layer)
	{
		foreach (var underFloorTile in matrix.MetaTileMap.GetAllTilesByType<BasicTile>(localPosition, layer))
		{
			var underFloorApply = new TileApply(
				performer, usedObject, intent, inMind, (Vector2Int)localPosition,
				this, underFloorTile, usedSlot, TargetPosition, applyType);

			foreach (var tileInteraction in underFloorTile.TileInteractions)
			{
				if (tileInteraction == null) continue;
				if (tileInteraction.WillInteract(underFloorApply, NetworkSide.Server))
				{
					PerformTileInteract(underFloorApply);
					break;
				}
			}
		}
	}

	private void PerformTileInteract(TileApply interaction)
	{
		foreach (var tileInteraction in interaction.BasicTile.TileInteractions)
		{
			if (tileInteraction == null) continue;
			if (tileInteraction.WillInteract(interaction, NetworkSide.Server))
			{
				//perform if not on cooldown
				if (Cooldowns.TryStartServer(interaction, CommonCooldowns.Instance.Interaction))
				{
					tileInteraction.ServerPerformInteraction(interaction);
				}
				else
				{
					//hit a cooldown, rollback in case client tried to predict it
					tileInteraction.ServerRollbackClient(interaction);
				}

				// interaction should've triggered and did or we hit a cooldown, so we're
				// done processing this request.
				break;
			}
			else
			{
				tileInteraction.ServerRollbackClient(interaction);
			}
		}
	}

	public bool Interact(MouseDrop interaction)
	{
		Loggy.Log("Interaction detected on InteractableTiles.", Category.Interaction);

		LayerTile tile = LayerTileAt(interaction.ShadowWorldLocation, true);

		if(tile is BasicTile basicTile)
		{
			var tileApply = new TileApply(interaction.Performer, interaction.UsedObject, interaction.Intent, interaction.PerformerMind, (Vector2Int)WorldToCell(interaction.ShadowWorldLocation), this, basicTile, null, interaction.ShadowWorldLocation.To3().ToLocal(interaction.Performer.RegisterTile().Matrix), TileApply.ApplyType.MouseDrop);
			var tileMouseDrop = new TileMouseDrop(interaction.Performer, interaction.UsedObject, interaction.Intent,  interaction.PerformerMind, (Vector2Int)WorldToCell(interaction.ShadowWorldLocation), this, basicTile, interaction.ShadowWorldLocation.To3().ToLocal(interaction.Performer.RegisterTile().Matrix));
			foreach (var tileInteraction in basicTile.TileInteractions)
			{
				if (tileInteraction == null) continue;
				if (tileInteraction.WillInteract(tileApply, NetworkSide.Client) &&
					Cooldowns.TryStartClient(interaction, CommonCooldowns.Instance.Interaction))
				{
					//request the tile interaction because we think one will happen
					RequestInteractMessage.SendTileMouseDrop(tileMouseDrop, this);
					return true;
				}
			}
		}

		return false;
	}

	public static bool instanceActive = false;
	public void OnHoverStart()
	{
		OnHover();
	}

	public void OnHover()
	{
		var wallMount = CheckWallMountOverlay();
		if (wallMount)
		{
			Vector2 cameraPos = MouseUtils.MouseToWorldPos();
			var tilePos = cameraPos.RoundToInt();
			OrientationEnum orientation = OrientationEnum.Down_By180;
			Vector3Int PlaceDirection = PlayerManager.LocalPlayerScript.WorldPos - tilePos;
			bool isWallBlocked = false;
			if (PlaceDirection.x != 0 && !MatrixManager.IsWallAt(tilePos + new Vector3Int(PlaceDirection.x > 0 ? 1 : -1, 0, 0), true))
			{
				if (PlaceDirection.x > 0)
				{
					orientation = OrientationEnum.Right_By270;
				}
				else
				{
					orientation = OrientationEnum.Left_By90;
				}
			}
			else
			{
				if (PlaceDirection.y != 0 && !MatrixManager.IsWallAt(tilePos + new Vector3Int(0, PlaceDirection.y > 0 ? 1 : -1, 0), true))
				{
					if (PlaceDirection.y > 0)
					{
						orientation = OrientationEnum.Up_By0;
					}
					else
					{
						orientation = OrientationEnum.Down_By180;
					}
				}
				else
				{
					isWallBlocked = true;
				}
			}

			if (!MatrixManager.IsWallAt(tilePos, false) || isWallBlocked)
			{
				if (instanceActive)
				{
					instanceActive = false;
					Highlight.DeHighlight();
				}
				return;
			}

			if (!instanceActive)
			{
				instanceActive = true;
				Highlight.ShowHighlight(PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot().ItemObject, true);
			}

			Vector3 spritePos = tilePos;
			if (wallMount.IsWallProtrusion) //for light bulbs, tubes, cameras, etc. move the sprite towards the floor
			{
				if(orientation == OrientationEnum.Right_By270)
				{
					spritePos.x += 0.5f;
					Highlight.instance.spriteRenderer.transform.rotation = Quaternion.Euler(0,0,270);
				}
				else if(orientation == OrientationEnum.Left_By90)
				{
						spritePos.x -= 0.5f;
						Highlight.instance.spriteRenderer.transform.rotation = Quaternion.Euler(0,0,90);
				}
				else if(orientation == OrientationEnum.Up_By0)
				{
					spritePos.y += 0.5f;
					Highlight.instance.spriteRenderer.transform.rotation = Quaternion.Euler(0,0,0);
				}
				else
				{
					spritePos.y -= 0.5f;
					Highlight.instance.spriteRenderer.transform.rotation = Quaternion.Euler(0,0,0);
				}
			}
			Highlight.instance.spriteRenderer.transform.position = spritePos;
		}
	}

	WallMountHandApplySpawn CheckWallMountOverlay()
	{

		var itemSlot = PlayerManager.LocalPlayerScript?.DynamicItemStorage?.GetActiveHandSlot();
		if (itemSlot == null || itemSlot.ItemObject == null)
		{
			return null;
		}
		var wallMount = itemSlot.ItemObject.GetComponent<WallMountHandApplySpawn>();
		return wallMount;
	}

	public void OnHoverEnd()
	{
		if (instanceActive)
		{
			instanceActive = false;
			Highlight.DeHighlight();
		}
	}

	/// <summary>
	/// Method for mining ore
	/// </summary>
	/// <param name="worldPosition"></param>
	/// <returns></returns>
	public bool TryMine(Vector3 worldPosition)
	{
		Vector3Int cellPos = metaTileMap.WorldToCell(worldPosition);

		var getTile = metaTileMap.GetTile(cellPos, LayerType.Walls) as BasicTile;
		if (getTile == null || getTile.Mineable == false) return false;

		SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.BreakStone, worldPosition);
		Spawn.ServerPrefab(getTile.SpawnOnDeconstruct, worldPosition,
			count: getTile.SpawnAmountOnDeconstruct);
		tileChangeManager.MetaTileMap.RemoveTileWithlayer(cellPos, LayerType.Walls);
		tileChangeManager.MetaTileMap.RemoveOverlaysOfType(cellPos, LayerType.Effects, OverlayType.Mining);

		return true;
	}

	/// <summary>
	/// Creates an animated tile in world position
	/// </summary>
	/// <param name="worldPosition"> Where to create tile </param>
	/// <param name="animatedTile"></param>
	/// <param name="animationTime"></param>
	public void CreateAnimatedTile(Vector3 worldPosition, AnimatedOverlayTile animatedTile, float animationTime)
	{
		Vector3Int cellPos = metaTileMap.WorldToCell(worldPosition);

		StartCoroutine(EnableAnimationCoroutine(cellPos, animatedTile, animationTime));
	}

	private IEnumerator EnableAnimationCoroutine(
		Vector3Int cellPos,
		AnimatedOverlayTile animatedTile,
		float animationTime)
	{
		tileChangeManager.MetaTileMap.AddOverlay(cellPos, animatedTile);

		yield return WaitFor.Seconds(animationTime);

		tileChangeManager.MetaTileMap.RemoveOverlaysOfType(cellPos, LayerType.Effects, animatedTile.OverlayType);
	}

}
