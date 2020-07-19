using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Class resposnible for handling cable cutting interactions
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CableCuttingWindow : MonoBehaviour
{
	[SerializeField] private GridLayoutGroup scrollViewLayoutGroup;
	[SerializeField] private GameObject cableCellUIPrefab;

	private RectTransform scrollViewLayoutGroupRect;

	/// <summary>
	/// Dictionary used to cache sprite atlas data
	/// </summary>
	private Dictionary<PowerTypeCategory, Sprite[]> cableSpritesDict;

	/// <summary>
	/// Paths to sprite atlases
	/// </summary>
	[SerializeField] private string HIGH_VOLTAGE_CABLE_PATH = "power_cond_high";
	[SerializeField] private string STANDARD_CABLE_PATH = "power_cond_red";
	[SerializeField] private string LOW_VOLTAGE_CABLE_PATH = "power_cond_low";

	/// <summary>
	/// Number of active cableCellUIPrefabs
	/// </summary>
	private int electricalCablesCount;

	/// <summary>
	/// Matrix on which target tile is placed
	/// </summary>
	private Matrix matrix;

	/// <summary>
	/// World position of target tile
	/// </summary>
	private Vector3 targetWorldPosition;

	/// <summary>
	/// Initialize values
	/// </summary>
	private void Awake()
	{
		scrollViewLayoutGroupRect = scrollViewLayoutGroup.GetComponent<RectTransform>();
		cableSpritesDict = new Dictionary<PowerTypeCategory, Sprite[]>();
	}

	/// <summary>
	/// Instantiate cableCellUIPrefab under scrollViewLayoutGroup and assign all necessary values
	/// </summary>
	private void InstantiateCableUICell(ElectricalCableTileData data)
	{
		// instantiate ui cell and get CableUICell script
		GameObject cableUI = Instantiate(cableCellUIPrefab, scrollViewLayoutGroup.transform);
		CableUICell cableUICell = cableUI.GetComponent<CableUICell>();

		// add on click listener
		cableUICell.cutWireButton.onClick.AddListener(() =>
		{
			CutWire(cableUI, data);
		});

		// set ui cell text
		cableUICell.wireLabelText.text = data.electricalCable.WireEndA + " - " + data.electricalCable.WireEndB;

		// set ui cell sprite
		int index = GetSpriteIndexByConnectionPoints(data.electricalCable.WireEndA, data.electricalCable.WireEndB);
		cableUICell.wireIconImage.sprite = GetSpriteAtlasByCableType(data.electricalCable.PowerType)[index];
	}

	/// <summary>
	/// Initialize window - find cables, instantiate ui cells, assign sprites
	/// </summary>
	/// <param name="matrix">Matrix on which target tile is placed</param>
	/// <param name="targetCellPosition">Cell position of target tile</param>
	/// <param name="targetWorldPosition">World position of target tile</param>
	public void InitializeCableCuttingWindow(Matrix matrix, Vector3Int targetCellPosition, Vector3 targetWorldPosition)
	{
		this.matrix = matrix;
		this.targetWorldPosition = targetWorldPosition;

		// destroy all old cable ui cells
		for (int i = 0; i < scrollViewLayoutGroup.transform.childCount; i++)
		{
			Destroy(scrollViewLayoutGroup.transform.GetChild(i).gameObject);
		}

		Vector3Int cellPos = targetCellPosition;
		electricalCablesCount = 0;

		// loop trough all layers searching for electrical cable tiles
		for (int i = 0; i < 50; i++)
		{
			cellPos.z = -i + 1;
			if (matrix.UnderFloorLayer.GetTileUsingZ(cellPos) is ElectricalCableTile electricalCableTile)
			{
				ElectricalCableTileData data = new ElectricalCableTileData
				{
					electricalCable = electricalCableTile,
					positionZ = cellPos.z
				};
				// instantiate ui object
				InstantiateCableUICell(data);
				electricalCablesCount++;
			}
		}

		// resize scroll view height after spawning objects
		CalculateRequiredWindowSize();
	}

	/// <summary>
	/// Load cable sprite atlas
	/// </summary>
	/// <param name="powerTypeCategory">cable power type</param>
	/// <returns></returns>
	private Sprite[] GetSpriteAtlasByCableType(PowerTypeCategory powerTypeCategory)
	{
		switch (powerTypeCategory)
		{
			case PowerTypeCategory.HighVoltageCable:
				// if this is first load - load sprite atlas from resources
				if (!cableSpritesDict.ContainsKey(powerTypeCategory))
				{
					cableSpritesDict.Add(powerTypeCategory, Resources.LoadAll<Sprite>(HIGH_VOLTAGE_CABLE_PATH));
				}
				// else - return cached sprite atlas
				return cableSpritesDict[powerTypeCategory];
			case PowerTypeCategory.StandardCable:
				if (!cableSpritesDict.ContainsKey(powerTypeCategory))
				{
					cableSpritesDict.Add(powerTypeCategory, Resources.LoadAll<Sprite>(STANDARD_CABLE_PATH));
				}
				return cableSpritesDict[powerTypeCategory];
			case PowerTypeCategory.LowVoltageCable:
				if (!cableSpritesDict.ContainsKey(powerTypeCategory))
				{
					cableSpritesDict.Add(powerTypeCategory, Resources.LoadAll<Sprite>(LOW_VOLTAGE_CABLE_PATH));
				}
				return cableSpritesDict[powerTypeCategory];
		}

		return null;
	}

	/// <summary>
	/// Get index of sprite in sprite atlas from two connection points
	/// </summary>
	/// <returns>Index of sprite in sprite atlas</returns>
	private int GetSpriteIndexByConnectionPoints(Connection wireEndA, Connection wireEndB)
	{
		switch (wireEndA)
		{
			case Connection.North:
				switch (wireEndB)
				{
					case Connection.NorthEast:
						return 10;
					case Connection.East:
						return 9;
					case Connection.SouthEast:
						return 11;
					case Connection.South:
						return 8;
					case Connection.SouthWest:
						return 14;
					case Connection.West:
						return 12;
					case Connection.NorthWest:
						return 13;
					case Connection.Overlap:
						return 0;
				}
				break;
			case Connection.NorthEast:
				switch (wireEndB)
				{
					case Connection.North:
						return 10;
					case Connection.East:
						return 21;
					case Connection.SouthEast:
						return 26;
					case Connection.South:
						return 16;
					case Connection.SouthWest:
						return 29;
					case Connection.West:
						return 27;
					case Connection.NorthWest:
						return 28;
					case Connection.Overlap:
						return 3;
				}
				break;
			case Connection.East:
				switch (wireEndB)
				{
					case Connection.North:
						return 9;
					case Connection.NorthEast:
						return 21;
					case Connection.SouthEast:
						return 22;
					case Connection.South:
						return 15;
					case Connection.SouthWest:
						return 25;
					case Connection.West:
						return 23;
					case Connection.NorthWest:
						return 24;
					case Connection.Overlap:
						return 2;
				}
				break;
			case Connection.SouthEast:
				switch (wireEndB)
				{
					case Connection.North:
						return 11;
					case Connection.NorthEast:
						return 26;
					case Connection.East:
						return 22;
					case Connection.South:
						return 17;
					case Connection.SouthWest:
						return 32;
					case Connection.West:
						return 30;
					case Connection.NorthWest:
						return 31;
					case Connection.Overlap:
						return 4;
				}
				break;
			case Connection.South:
				switch (wireEndB)
				{
					case Connection.North:
						return 8;
					case Connection.NorthEast:
						return 16;
					case Connection.East:
						return 15;
					case Connection.SouthEast:
						return 17;
					case Connection.SouthWest:
						return 20;
					case Connection.West:
						return 18;
					case Connection.NorthWest:
						return 19;
					case Connection.Overlap:
						return 1;
				}
				break;
			case Connection.SouthWest:
				switch (wireEndB)
				{
					case Connection.North:
						return 14;
					case Connection.NorthEast:
						return 29;
					case Connection.East:
						return 25;
					case Connection.SouthEast:
						return 32;
					case Connection.South:
						return 20;
					case Connection.West:
						return 34;
					case Connection.NorthWest:
						return 35;
					case Connection.Overlap:
						return 7;
				}
				break;
			case Connection.West:
				switch (wireEndB)
				{
					case Connection.North:
						return 12;
					case Connection.NorthEast:
						return 27;
					case Connection.East:
						return 23;
					case Connection.SouthEast:
						return 30;
					case Connection.South:
						return 18;
					case Connection.SouthWest:
						return 34;
					case Connection.NorthWest:
						return 33;
					case Connection.Overlap:
						return 5;
				}
				break;
			case Connection.NorthWest:
				switch (wireEndB)
				{
					case Connection.North:
						return 13;
					case Connection.NorthEast:
						return 28;
					case Connection.East:
						return 24;
					case Connection.SouthEast:
						return 31;
					case Connection.South:
						return 19;
					case Connection.SouthWest:
						return 35;
					case Connection.West:
						return 33;
					case Connection.Overlap:
						return 6;
				}
				break;
			case Connection.Overlap:
				switch (wireEndB)
				{
					case Connection.North:
						return 0;
					case Connection.NorthEast:
						return 3;
					case Connection.East:
						return 2;
					case Connection.SouthEast:
						return 4;
					case Connection.South:
						return 1;
					case Connection.SouthWest:
						return 7;
					case Connection.West:
						return 5;
					case Connection.NorthWest:
						return 6;
				}
				break;
		}

		return 0;
	}

	/// <summary>
	/// Calculate scroll view height based on electricalCablesCount
	/// </summary>
	private void CalculateRequiredWindowSize()
	{
		float height = scrollViewLayoutGroup.padding.top + scrollViewLayoutGroup.padding.bottom;
		height += electricalCablesCount * scrollViewLayoutGroup.cellSize.y;
		height += (electricalCablesCount - 1) * scrollViewLayoutGroup.spacing.y;

		scrollViewLayoutGroupRect.sizeDelta = new Vector2(scrollViewLayoutGroupRect.sizeDelta.x, height);
	}

	/// <summary>
	/// Method used to enable/disable this window
	/// </summary>
	/// <param name="active">enable = true / disable = false</param>
	public void SetWindowActive(bool active)
	{
		gameObject.SetActive(active);
	}

	/// <summary>
	/// Method called on button click, used to remove wires from world
	/// </summary>
	/// <param name="cableUI">cable ui cell which calling this method</param>
	/// <param name="data">electircal cable tile data</param>
	public void CutWire(GameObject cableUI, ElectricalCableTileData data)
	{
		if (data.electricalCable == null) return;

		Vector3 targetVec = targetWorldPosition - PlayerManager.LocalPlayer.transform.position;
		var apply = PositionalHandApply.ByLocalPlayer(matrix.gameObject, targetVec);

		// if can interact and there are no cooldown - send message to server and destroy UI cell
		if (WillInteract(apply) && Cooldowns.TryStartClient(apply, CommonCooldowns.Instance.Interaction))
		{
			// send message to destroy cable
			SendCableCuttingMessage(targetWorldPosition, data.positionZ);
			// destroy ui object
			Destroy(cableUI);
			// decrease cable count
			electricalCablesCount--;
			// resize scroll view height
			CalculateRequiredWindowSize();

			// disable window if there are no objects in scrollview
			if (electricalCablesCount == 0)
				SetWindowActive(false);
		}
	}

	/// <summary>
	/// Check if interaction is possible
	/// </summary>
	private bool WillInteract(PositionalHandApply apply)
	{
		if (!DefaultWillInteract.Default(apply, NetworkSide.Client)) return false;
		return Validations.HasItemTrait(UIManager.Hands.CurrentSlot.ItemObject, CommonTraits.Instance.Wirecutter);
	}

	/// <summary>
	/// [Send Message] Send message to server to cut specified wire
	/// </summary>
	/// <param name="targetWorldPosition">World position of target tile</param>
	/// <param name="positionZ">Z position of target tile</param>
	private void SendCableCuttingMessage(Vector3 targetWorldPosition, int positionZ)
	{
		// create  message
		CableCuttingMessage message = new CableCuttingMessage()
		{
			performer = PlayerManager.LocalPlayer,
			targetWorldPosition = targetWorldPosition,
			positionZ = positionZ
		};

		// send message
		NetworkClient.Send(message, 0);
	}

	/// <summary>
	/// Struct containing all data needed to process cutting interaction
	/// </summary>
	public struct ElectricalCableTileData
	{
		public int positionZ;
		public ElectricalCableTile electricalCable;
	}

	/// <summary>
	/// Message containing data needed for cutting cables
	/// </summary>
	public class CableCuttingMessage : MessageBase
	{
		public GameObject performer;
		public Vector3 targetWorldPosition;
		public int positionZ;
	}
}
