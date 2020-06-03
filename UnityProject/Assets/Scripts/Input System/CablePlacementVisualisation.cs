using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// [Client Side Only] MonoBehaviour for handling cable placement visualisation
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class CablePlacementVisualisation : MonoBehaviour
{
	private const int gridSize = 3;
	private const int maxIndex = gridSize - 1;

	/// <summary>
	/// color of startPoint(point on which you press mouse button down)
	/// </summary>
	[SerializeField] private Color startPointColor;
	/// <summary>
	/// color of currentlyHoveredPoint(point below mouse)
	/// </summary>
	[SerializeField] private Color onHoverPointColor;

	/// <summary>
	/// point on which player has pressed mouse button
	/// </summary>
	private Vector2Int startPoint;
	/// <summary>
	/// point on which player has released mouse button
	/// </summary>
	private Vector2Int endPoint;
	/// <summary>
	/// point that is currently below mouse
	/// </summary>
	private Vector2Int currentlyHoveredPoint;

	/// <summary>
	/// grid of points
	/// </summary>
	private SpriteRenderer[,] connectionPoints = new SpriteRenderer[gridSize, gridSize];
	/// <summary>
	/// lineRenderer used to render line between points
	/// </summary>
	private LineRenderer lineRenderer;

	/// <summary>
	/// variable used to restore default color after hover
	/// </summary>
	private Color defaultPointColor;

	private void Awake()
	{
		// initialize grid
		for (int y = 0; y < gridSize; y++)
		{
			for (int x = 0; x < gridSize; x++)
			{
				connectionPoints[x, y] = transform.GetChild(y * gridSize + x).GetComponent<SpriteRenderer>();
			}
		}

		// get default color from first point
		defaultPointColor = connectionPoints[0, 0].color;
		// get line renderer
		lineRenderer = GetComponent<LineRenderer>();
	}

	private void OnEnable()
	{
		// reset positions on enable
		ResetValues();
	}

	private void Update()
	{
		// get releative mouse position
		Vector2 releativeMousePosition = Camera.main.ScreenToWorldPoint(CommonInput.mousePosition) - transform.position;
		// get nearest point
		int x = Mathf.RoundToInt(releativeMousePosition.x * maxIndex);
		int y = gridSize - 1 - Mathf.RoundToInt(releativeMousePosition.y * maxIndex);

		// clamp to be sure that value is in range <0, maxIndex>
		x = Mathf.Clamp(x, 0, maxIndex);
		y = Mathf.Clamp(y, 0, maxIndex);
		Vector2Int point = new Vector2Int(x, y);

		// if hover point has changed
		if (point != currentlyHoveredPoint)
		{
			// check if current point isn't startPoint or endPoint (to not override color)
			if (currentlyHoveredPoint != startPoint && currentlyHoveredPoint != endPoint)
				SetConnectionPointColor(currentlyHoveredPoint.x, currentlyHoveredPoint.y, defaultPointColor);

			SetConnectionPointColor(point.x, point.y, onHoverPointColor);
			currentlyHoveredPoint = point;
		}

		// if mouse down - start drawing
		if (CommonInput.GetMouseButtonDown(0))
		{
			startPoint = currentlyHoveredPoint;
			SetConnectionPointColor(startPoint.x, startPoint.y, startPointColor);
		}
		// if mouse up - stop drawing and check if can build
		else if (CommonInput.GetMouseButtonUp(0))
		{
			endPoint = currentlyHoveredPoint;
			// TODO: check if correct and build
			Build();
			ResetValues();
		}

		// check if startPoint has changed (-Vector2Int.one is default value)
		if (startPoint != -Vector2Int.one)
		{
			lineRenderer.SetPositions(new Vector3[]
			{
				connectionPoints[startPoint.x, startPoint.y].transform.localPosition,                        // position of start point
                connectionPoints[currentlyHoveredPoint.x, currentlyHoveredPoint.y].transform.localPosition   // position of current point
            });
		}
	}

	/// <summary>
	/// Try to build cable
	/// </summary>
	private void Build()
	{
		if (startPoint == endPoint || Mathf.Abs(startPoint.x - endPoint.x) > 1 || Mathf.Abs(startPoint.y - endPoint.y) > 1) return;

		GameObject target = MouseUtils.GetOrderedObjectsUnderMouse().FirstOrDefault();
		Vector2 targetVector = new Vector2(startPoint.x - endPoint.x, startPoint.y - endPoint.y);


		Connection start = GetConnectionByPoint(startPoint);
		Connection end = GetConnectionByPoint(endPoint);
		CableApply cableApply = CableApply.ByLocalPlayer(target, start, end, Vector2Int.RoundToInt(transform.position + new Vector3(0.5f, 0.5f, 0)));

		//if HandObject is null, then its an empty hand apply so we only need to check the receiving object
		if (cableApply.HandObject != null)
		{
			//get all components that can contains CableApply interaction
			var cableAppliables = cableApply.HandObject.GetComponents<MonoBehaviour>()
				.Where(c => c != null && c.enabled && (c is IBaseInteractable<CableApply>));

			foreach (var cableAppliable in cableAppliables.Reverse())
			{
				var hap = cableAppliable as IBaseInteractable<CableApply>;
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
		foreach (var point in connectionPoints)
			point.color = defaultPointColor;

		// reset points
		startPoint = -Vector2Int.one;
		endPoint = -Vector2Int.one;
	}

	/// <summary>
	/// Change color of connection point
	/// </summary>
	/// <param name="x">x position</param>
	/// <param name="y">y position</param>
	/// <param name="color">target color</param>
	private void SetConnectionPointColor(int x, int y, Color color)
	{
		// get sprite renderer
		SpriteRenderer spriteRenderer = connectionPoints[x, y];
		// change color
		spriteRenderer.color = color;
	}
	/// <summary>
	/// Convert Vector2Int point to Connection based on position
	/// (point values should be in range from 0 to <see cref="maxIndex"/>)
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
}
