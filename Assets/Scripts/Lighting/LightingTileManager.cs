﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lighting
{
	public class LightingTileManager : MonoBehaviour
	{
		public Dictionary<Vector2,LightTile> lightTiles = new Dictionary<Vector2, LightTile>();

		public Vector4 bounds;

		void Start()
		{
			LoadLightTiles();
		}

		void LoadLightTiles()
		{
			foreach (Transform child in transform) {
				LightTile lightTile = child.gameObject.GetComponent<LightTile>();
				if (lightTile != null) {
					if (!lightTiles.ContainsKey(child.transform.position)) {
						lightTiles.Add(child.transform.position, lightTile);
					} else {
						Destroy(child.gameObject);
					}
				} else {
					Debug.LogError("No LightTile component found!");
				}
			}

			CalculateBounds();
		}

		//Calculate the bounds of the lightTiles in the room
		void CalculateBounds()
		{
			bounds = Vector4.zero;
			Vector2 topLeft = Vector2.zero;
			Vector2 bottomRight = Vector2.zero;
			foreach (KeyValuePair<Vector2,LightTile> key in lightTiles) {
				//starting point
				if (topLeft == Vector2.zero) {
					topLeft = key.Key;
					bottomRight = key.Key;
				} 

				topLeft = CompareTopLeft(topLeft, key.Key);
				bottomRight = CompareBottomRight(bottomRight, key.Key);
			}

			bounds = new Vector4(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);

		}

		Vector2 CompareTopLeft(Vector2 curValue, Vector2 newValue){
	
			if (newValue.x < curValue.x) {
				curValue.x = newValue.x;
			}
			if(newValue.y > curValue.y){
				curValue.y = newValue.y;
			}
			return curValue;
		}

		Vector2 CompareBottomRight(Vector2 curValue, Vector2 newValue){

			if (newValue.x > curValue.x) {
				curValue.x = newValue.x;
			}
			if(newValue.y < curValue.y){
				curValue.y = newValue.y;
			}
			return curValue;
		}
	}
}
