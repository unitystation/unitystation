using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// [Client Side Only] MonoBehaviour for handling cable placement visualisation
/// </summary>
public class CablePlacementVisualisation : MonoBehaviour
{
	/// <summary>
	/// Prefab used to visualise cable placement
	/// </summary>
	[SerializeField] private GameObject cablePlacementVisualisationPrefab;
	private GameObject cablePlacementVisualisation;

	/// <summary>
	/// color of startPoint(point on which you press mouse button down)
	/// </summary>
	[SerializeField] private Color startPointColor;
	/// <summary>
	/// color of point below mouse
	/// </summary>
	[SerializeField] private Color onHoverPointColor;

	/// <summary>
	/// point on which player has pressed mouse button
	/// </summary>
	private Connection startPoint;
	/// <summary>
	/// point on which player has released mouse button
	/// </summary>
	private Connection endPoint;
	/// <summary>
	/// point that was below mouse in last update
	/// </summary>
	private Connection lastConnection;

	/// <summary>
	/// grid of points
	/// </summary>
	private Dictionary<Connection, SpriteRenderer> connectionPointRenderers;
	/// <summary>
	/// lineRenderer used to render line between points
	/// </summary>
	private LineRenderer lineRenderer;

	/// <summary>
	/// variable used to restore default color after hover
	/// </summary>
	private Color defaultPointColor;

	/// <summary>
	/// last mouse position used in OnHover() to determine if player hovers other tile
	/// </summary>
	private Vector3Int lastMouseWordlPositionInt;
	/// <summary>
	/// The target tile the cable will be placed on.
	/// </summary>
	private GameObject target;

	private void Awake()
	{
		// instantiate prefab
		cablePlacementVisualisation = Instantiate(cablePlacementVisualisationPrefab);
		cablePlacementVisualisation.SetActive(false);

		// init grid
		connectionPointRenderers = new Dictionary<Connection, SpriteRenderer>();
		for (int i = 0; i < 9; i++)
		{
			connectionPointRenderers[(Connection)i + 1] = cablePlacementVisualisation.transform.GetChild(i).GetComponent<SpriteRenderer>();
		}

		// get default color from first point
		defaultPointColor = connectionPointRenderers[Connection.Overlap].color;
		// get line renderer
		lineRenderer = cablePlacementVisualisation.GetComponent<LineRenderer>();
	}

	private void Update()
	{
		// return if visualisation is disabled or distance is greater than interaction distance
		if (!cablePlacementVisualisation.activeSelf) return;

		// get releative mouse position
		Vector2 releativeMousePosition = Camera.main.ScreenToWorldPoint(CommonInput.mousePosition) - cablePlacementVisualisation.transform.position;
		// get nearest point
		int x = Mathf.RoundToInt(releativeMousePosition.x * 2);
		int y = 2 - Mathf.RoundToInt(releativeMousePosition.y * 2);

		// clamp to be sure that value is in range <0, 2>
		x = Mathf.Clamp(x, 0, 2);
		y = Mathf.Clamp(y, 0, 2);
		Vector2Int point = new Vector2Int(x, y);

		Connection currentConnection = GetConnectionByPoint(point);

		// if mouse down - start drawing
		if (CommonInput.GetMouseButtonDown(0))
		{
			startPoint = currentConnection;
			target = MouseUtils.GetOrderedObjectsUnderMouse().FirstOrDefault();

			SetConnectionPointColor(startPoint, startPointColor);
		}
		// if mouse up - stop drawing and check if can build
		else if (CommonInput.GetMouseButtonUp(0))
		{
			endPoint = currentConnection;
			Build();
			ResetValues();
			return;
		}

		// check if position has changed
		if (currentConnection != lastConnection)
		{
			// check if last point isn't startPoint or endPoint (to not override color)
			if (lastConnection != startPoint && lastConnection != endPoint)
				SetConnectionPointColor(lastConnection, defaultPointColor);

			SetConnectionPointColor(currentConnection, onHoverPointColor);

			if (startPoint != Connection.NA)
			{
				lineRenderer.SetPositions(new Vector3[]
				{
					connectionPointRenderers[startPoint].transform.localPosition,		// position of start point
					connectionPointRenderers[currentConnection].transform.localPosition	// position of current point
				});
			}

			lastConnection = currentConnection;
		}
	}

	/// <summary>
	/// Try to build cable
	/// </summary>
	private void Build()
	{
		if (startPoint == endPoint || target == null) return;

		Vector2 targetVector = (connectionPointRenderers[endPoint].transform.position - transform.position);
		ConnectionApply cableApply = ConnectionApply.ByLocalPlayer(target, startPoint, endPoint, targetVector);

		//if HandObject is null, then its an empty hand apply so we only need to check the receiving object
		if (cableApply.HandObject != null)
		{
			//get all components that can contains CableApply interaction
			var cableAppliables = cableApply.HandObject.GetComponents<MonoBehaviour>()
				.Where(c => c != null && c.enabled && (c is IBaseInteractable<ConnectionApply>));

			foreach (var cableAppliable in cableAppliables.Reverse())
			{
				var hap = cableAppliable as IBaseInteractable<ConnectionApply>;
				if (hap.ClientCheckAndTrigger(cableApply)) return;
			}
		}
	}

	/// <summary>
	/// Reset startPoint, endPoint and lineRenderer
	/// </summary>
	private void ResetValues()
	{
		// reset line renderer
		lineRenderer.SetPositions(new Vector3[]
		{
			Vector3.zero,
			Vector3.zero
		});


		// reset colors
		foreach (var point in connectionPointRenderers.Values)
			point.color = defaultPointColor;

		// reset points
		startPoint = Connection.NA;
		endPoint = Connection.NA;

		lastMouseWordlPositionInt = Vector3Int.zero;
		lastConnection = Connection.NA;

		target = null;
	}

	private void DisableVisualisation()
	{
		if (cablePlacementVisualisation.activeSelf)
		{
			cablePlacementVisualisation.SetActive(false);
			ResetValues();
		}
	}

	/// <summary>
	/// Change color of connection point
	/// </summary>
	/// <param name="x">x position</param>
	/// <param name="y">y position</param>
	/// <param name="color">target color</param>
	private void SetConnectionPointColor(Connection connection, Color color)
	{
		// get sprite renderer and set color
		connectionPointRenderers[connection].color = color;
	}

	/// <summary>
	/// Convert Vector2Int point to Connection based on position
	/// (point values should be in range <0, 2>)
	/// </summary>
	/// <param name="point">target point</param>
	/// <returns></returns>
	private Connection GetConnectionByPoint(Vector2Int point)
	{
		int x = point.x;
		int y = point.y;
		switch (x)
		{
			// west
			case 0:
				switch (y)
				{
					case 2:
						return Connection.SouthWest;
					case 1:
						return Connection.West;
					case 0:
						return Connection.NorthWest;
				}
				return Connection.NA;
			// center
			case 1:
				switch (y)
				{
					case 2:
						return Connection.South;
					case 1:
						return Connection.Overlap;
					case 0:
						return Connection.North;
				}
				return Connection.NA;
			// east
			case 2:
				switch (y)
				{
					case 2:
						return Connection.SouthEast;
					case 1:
						return Connection.East;
					case 0:
						return Connection.NorthEast;
				}
				return Connection.NA;
			default:
				return Connection.NA;
		}
	}

	public void OnHover()
	{
		if (!UIManager.IsMouseInteractionDisabled && UIManager.Hands.CurrentSlot != null)
		{
			// get mouse position
			Vector3 mousePosition = Camera.main.ScreenToWorldPoint(CommonInput.mousePosition);
			// round mouse position
			Vector3Int roundedMousePosition = Vector3Int.RoundToInt(mousePosition);

			// if distance is greater than interaction distance
			if (Vector2.Distance(transform.position, (Vector3)roundedMousePosition) > PlayerScript.interactionDistance)
			{
				DisableVisualisation();
				return;
			}

			// if position has changed and player has cable in hand
			if (roundedMousePosition != lastMouseWordlPositionInt
				&& Validations.HasItemTrait(UIManager.Hands.CurrentSlot.ItemObject, CommonTraits.Instance.Cable))
			{
				lastMouseWordlPositionInt = roundedMousePosition;

				// get metaTileMap and top tile
				// MetaTileMap metaTileMap = MatrixManager.AtPoint(roundedMousePosition, false).MetaTileMap;
				// LayerTile topTile = metaTileMap.GetTile(metaTileMap.WorldToCell(mousePosition), true);
				// *code above works only on Station matrix
				// TODO: replace GetComponent solution with some built-in method?

				var hit = MouseUtils.GetOrderedObjectsUnderMouse().FirstOrDefault();
				MetaTileMap metaTileMap = hit.GetComponentInChildren<MetaTileMap>();
				if (metaTileMap)
				{
					LayerTile topTile = metaTileMap.GetTile(metaTileMap.WorldToCell(roundedMousePosition), true);
					if (topTile && (topTile.LayerType == LayerType.Base || topTile.LayerType == LayerType.Underfloor))
					{
						// move cable placement visualisation to rounded mouse position and enable it
						cablePlacementVisualisation.transform.position = roundedMousePosition - new Vector3(0.5f, 0.5f, 0); ;
						cablePlacementVisualisation.SetActive(true);
					}
					// disable visualisation if active
					else
						DisableVisualisation();
				}
				else
					DisableVisualisation();
			}
		}
		else
			DisableVisualisation();
	}
}
