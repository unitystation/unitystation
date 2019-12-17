using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;
using UnityEngine;

public class ParallaxController : MonoBehaviour
{
	public List<ParallaxColumn> backgroundTiles;
	private Vector2 tileBounds;
	private int centerColumn;
	
	void Awake()
	{
		var rend = backgroundTiles[0].rows[0].GetComponent<SpriteRenderer>();
		tileBounds = (rend.sprite.bounds.size * rend.sprite.pixelsPerUnit) / 100f;
		centerColumn = backgroundTiles.Count / 2;
		RealignTiles();
	}

	void RealignTiles()
	{
		for (int i = 0; i < backgroundTiles.Count; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				Vector3 newPos = Vector3.zero;
				newPos.x = (i - centerColumn) * tileBounds.x;
				newPos.y = (1 - j) * tileBounds.y;
				backgroundTiles[i].rows[j].transform.localPosition = newPos;
			}
		}
	}

	void Update()
	{

	}
}

[Serializable]
public class ParallaxColumn
{
	public List<ParallaxStars> rows = new List<ParallaxStars>();
}