using System;
using System.Collections.Generic;
using Logs;
using UnityEngine;
using Random = System.Random;

public class ParallaxController : MonoBehaviour
{
	public ParallaxStars PrefabParallax;

	public int Columns;
	public int Rows;

	public List<ParallaxColumn> backgroundTiles;


	public List<ParallaxStars> PrePlaced;

	public Vector2 tileBounds;
	private int centerColumn;
	private int centerRow;

	public Vector3 ObjectScale = Vector3.one;

	public float moveMultiplier = 1;

	public float zOffset = -1;

	void Awake()
	{
		for (int i = 0; i < Columns; i++)
		{
			var Column = new ParallaxColumn();
			backgroundTiles.Add(Column);
			for (int j = 0; j < Rows; j++)
			{
				var Object = Instantiate(PrefabParallax, transform);
				Object.transform.localScale = ObjectScale;
				Column.rows.Add(Object);
			}
		}
		Random random = new Random();
		foreach (var PrePlace in PrePlaced)
		{
			int randomColumn = random.Next(Columns);
			int randomRow = random.Next(Rows);
			Destroy(backgroundTiles[randomColumn].rows[randomRow].gameObject);
			backgroundTiles[randomColumn].rows[randomRow] = PrePlace;
		}

		var rend = backgroundTiles[0].rows[0].GetComponent<SpriteRenderer>();
		if (rend != null)
		{
			tileBounds = ((rend.sprite.bounds.size * rend.sprite.pixelsPerUnit) / 100f) * rend.transform.localScale.To2() ;
		}
		else
		{
			tileBounds = backgroundTiles[0].rows[0].transform.localScale.To2();
		}

		centerColumn = Columns / 2;
		centerRow = Rows / 2;
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

	[NaughtyAttributes.Button]
	public void Rescale()
	{
		for (int i = 0; i < backgroundTiles.Count; i++)
		{
			for (int j = 0; j < backgroundTiles[0].rows.Count; j++)
			{
				backgroundTiles[i].rows[j].transform.localScale = ObjectScale;
			}
		}

		var rend = backgroundTiles[0].rows[0].GetComponent<SpriteRenderer>();
		if (rend != null)
		{
			tileBounds = ((rend.sprite.bounds.size * rend.sprite.pixelsPerUnit) / 100f) * rend.transform.localScale.To2() ;
		}
		else
		{
			tileBounds = backgroundTiles[0].rows[0].transform.localScale.To2();
		}

		centerColumn = Columns / 2;
		centerRow = Rows / 2;

		RealignTilesEditor();
	}


	[NaughtyAttributes.Button]
	public void RealignTilesEditor()
	{
		RealignTiles(true);
		RealignTiles(false);
	}

	void RealignTiles(bool? UpdateX = null)
	{
		// this.transform.position = Camera.main.transform.position;
		for (int i = 0; i < backgroundTiles.Count; i++)
		{
			for (int j = 0; j < backgroundTiles[0].rows.Count; j++)
			{
				Vector3 newPos = Camera.main.transform.position;
				newPos.z = zOffset;
				if (UpdateX is true or null)
				{
					newPos.x = Camera.main.transform.position.x;
					newPos.x += (i - centerColumn) * tileBounds.x;
				}
				else
				{
					newPos.x = backgroundTiles[i].rows[j].transform.position.x;
				}


				if (UpdateX is false or null)
				{
					newPos.y = Camera.main.transform.position.y;
					newPos.y += (j - centerRow) * tileBounds.y;
				}
				else
				{
					newPos.y = backgroundTiles[i].rows[j].transform.position.y;
				}

				backgroundTiles[i].rows[j].transform.position = newPos;
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
		transform.position = Camera.main.transform.position * moveMultiplier;

		if ((Camera.main.transform.position.x - backgroundTiles[centerColumn].rows[centerRow].transform.position.x) > tileBounds.x)
		{
			MoveTiles(Vector2.right);
		}

		if ((Camera.main.transform.position.x
		     - backgroundTiles[centerColumn].rows[centerRow].transform.position.x) < -tileBounds.x)
		{
			MoveTiles(Vector2.left);
		}

		if ((Camera.main.transform.position.y
		     - backgroundTiles[centerColumn].rows[centerRow].transform.position.y) > tileBounds.y)
		{
			MoveTiles(Vector2.up);
		}

		if ((Camera.main.transform.position.y
		     - backgroundTiles[centerColumn].rows[centerRow].transform.position.y) < -tileBounds.y)
		{
			MoveTiles(Vector2.down);
		}
	}

	void MoveTiles(Vector2 direction)
	{
		bool? UpdateX = null;
		if (direction == Vector2.up)
		{
			UpdateX = false;
			foreach (ParallaxColumn c in backgroundTiles)
			{
				var from = c.rows[^1];
				c.rows.Remove(from);
				c.rows.Insert(0, from);
			}
		}
		else if (direction == Vector2.down)
		{
			UpdateX = false;
			foreach (ParallaxColumn c in backgroundTiles)
			{
				var from = c.rows[0];
				c.rows.Remove(from);
				c.rows.Add(from);
			}
		}
		else if (direction == Vector2.right)
		{
			UpdateX = true;
			var from = backgroundTiles[0];
			backgroundTiles.Remove(from);
			backgroundTiles.Add(from);
		}
		else if (direction == Vector2.left)
		{
			UpdateX = true;
			var from = backgroundTiles[^1];
			backgroundTiles.Remove(from);
			backgroundTiles.Insert(0, from);
		}
		RealignTiles(UpdateX);
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