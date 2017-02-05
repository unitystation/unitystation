using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lighting
{
	public class LightTile : MonoBehaviour
	{

		public SpriteRenderer thisSprite{get; set;}
		private LightingTileManager tileManager;
		private bool transmitting = false;
		void Start()
		{
			thisSprite = GetComponentInChildren<SpriteRenderer>();
			tileManager = GetComponentInParent<LightingTileManager>();
		}

		//If this tile touches a light source then absorb the light and send to surrounds
		public void InjectLight(float brightness){
			if (!transmitting) {
				transmitting = true;
				float leftOverLight = brightness - 10f;

				if (leftOverLight >= 0f) {
					var tempColor = thisSprite.color;
					tempColor.a = 0f;
					thisSprite.color = tempColor;

					StartCoroutine("PassTheLight", leftOverLight);
				}
			}
		}

		//Pass the brightness of the light to neighbor tiles
		IEnumerator PassTheLight(float _brightness){

			Vector2 curPos = transform.position;
			//Check up first
			Vector2 checkPos = new Vector2(curPos.x, curPos.y + 1f);
			CheckNeighbor(checkPos, _brightness);
			yield return new WaitForEndOfFrame();
			//Check down 
			checkPos = new Vector2(curPos.x, curPos.y - 1f);
			CheckNeighbor(checkPos, _brightness);
			yield return new WaitForEndOfFrame();
			//Check left 
			checkPos = new Vector2(curPos.x - 1f, curPos.y);
			CheckNeighbor(checkPos, _brightness);
			yield return new WaitForEndOfFrame();
			//Check right
			checkPos = new Vector2(curPos.x + 1f, curPos.y);
			CheckNeighbor(checkPos, _brightness);
			yield return new WaitForEndOfFrame();
			//Check Up Right corner
			checkPos = new Vector2(curPos.x + 1f, curPos.y + 1f);
			CheckNeighbor(checkPos, _brightness);
			yield return new WaitForEndOfFrame();
			//Check Up Left corner
			checkPos = new Vector2(curPos.x - 1f, curPos.y + 1f);
			CheckNeighbor(checkPos, _brightness);
			yield return new WaitForEndOfFrame();
			//Check Bottom Left corner
			checkPos = new Vector2(curPos.x - 1f, curPos.y - 1f);
			CheckNeighbor(checkPos, _brightness);
			yield return new WaitForEndOfFrame();
			//Check Bottom Right corner
			checkPos = new Vector2(curPos.x + 1f, curPos.y - 1f);
			CheckNeighbor(checkPos, _brightness);
			transmitting = false;

		}

		private void CheckNeighbor(Vector2 tileNextPos, float _brightness){
			if (tileManager.lightTiles.ContainsKey(tileNextPos)) {
				tileManager.lightTiles[tileNextPos].InjectLight(_brightness);
			}
		}
	}
}