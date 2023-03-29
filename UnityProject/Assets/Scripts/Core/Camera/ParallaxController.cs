using System;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxController : MonoBehaviour
{
	public List<ParallaxColumn> backgroundTiles;
	private Vector2 tileBounds;
	private int centerColumn;
	private int centerRow;

	void Awake()
	{
		var rend = backgroundTiles[0].rows[0].GetComponent<SpriteRenderer>();
		tileBounds = (rend.sprite.bounds.size * rend.sprite.pixelsPerUnit) / 100f;
		centerColumn = backgroundTiles.Count / 2;
		centerRow = backgroundTiles[0].rows.Count / 2;
		RealignTiles();
	}

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	void RealignTiles()
	{
		for (int i = 0; i < backgroundTiles.Count; i++)
		{
			for (int j = 0; j < backgroundTiles[0].rows.Count; j++)
			{
				Vector3 newPos = backgroundTiles[centerColumn].rows[centerRow].transform.localPosition;
				newPos.x += (i - centerColumn) * tileBounds.x;
				newPos.y += (centerRow - j) * tileBounds.y;
				backgroundTiles[i].rows[j].transform.localPosition = newPos;
			}
		}

		CheckSpaceBackgroundObjects();
	}

	void UpdateMe()
	{
		if (Manager3D.Is3D) return;
		MonitorTiles();
	}

	void MonitorTiles()
	{
		if ((Camera.main.transform.position.x
		     - backgroundTiles[centerColumn].rows[centerRow].transform.position.x) > 20f)
		{
			MoveTiles(Vector2.right);
		}

		if ((Camera.main.transform.position.x
		     - backgroundTiles[centerColumn].rows[centerRow].transform.position.x) < -20f)
		{
			MoveTiles(Vector2.left);
		}

		if ((Camera.main.transform.position.y
		     - backgroundTiles[centerColumn].rows[centerRow].transform.position.y) > 20f)
		{
			MoveTiles(Vector2.up);
		}

		if ((Camera.main.transform.position.y
		     - backgroundTiles[centerColumn].rows[centerRow].transform.position.y) < -20f)
		{
			MoveTiles(Vector2.down);
		}
	}

	void MoveTiles(Vector2 direction)
	{
		if (direction == Vector2.up)
		{
			foreach (ParallaxColumn c in backgroundTiles)
			{
				var from = c.rows[2];
				c.rows.Remove(from);
				c.rows.Insert(0, from);
			}
		}
		else if (direction == Vector2.down)
		{
			foreach (ParallaxColumn c in backgroundTiles)
			{
				var from = c.rows[0];
				c.rows.Remove(from);
				c.rows.Add(from);
			}
		}
		else if (direction == Vector2.right)
		{
			var from = backgroundTiles[0];
			backgroundTiles.Remove(from);
			backgroundTiles.Add(from);
		}
		else if (direction == Vector2.left)
		{
			var from = backgroundTiles[backgroundTiles.Count - 1];
			backgroundTiles.Remove(from);
			backgroundTiles.Insert(0, from);
		}
		RealignTiles();
	}

	void CheckSpaceBackgroundObjects()
	{
		foreach (ParallaxColumn c in backgroundTiles)
		{
			foreach (ParallaxStars s in c.rows)
			{
				s.CheckSpaceObjects();
			}
		}
	}
}

[Serializable]
public class ParallaxColumn
{
	public List<ParallaxStars> rows = new List<ParallaxStars>();
}