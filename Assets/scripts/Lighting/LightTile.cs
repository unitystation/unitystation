using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lighting
{
	public class LightTile : MonoBehaviour
	{

		public SpriteRenderer thisSprite{ get; set; }

		private LightingTileManager tileManager;
		private bool transmitting = false;

		void Start()
		{
			thisSprite = GetComponentInChildren<SpriteRenderer>();
			tileManager = GetComponentInParent<LightingTileManager>();
		}

		//If this tile touches a light source then absorb the light and send to surrounds
		public void InjectLight(float brightness, int range)
		{
			if (!transmitting) {
				transmitting = true;
				float alpha = Mathf.Abs((100f - brightness) / 100f);
				if (range >= 0f) {
					
					var tempColor = thisSprite.color;
					tempColor.a = alpha;
					thisSprite.color = tempColor;
					StartCoroutine(PassTheLight(brightness, range));
				} else {
					transmitting = false;
				}
			}
		}

		//Pass the brightness of the light to neighbor tiles
		IEnumerator PassTheLight(float brightness, int range)
		{
			range--;
			Vector2 curPos = transform.position;
			//Check up first
			Vector2 checkPos = new Vector2(curPos.x, curPos.y + 1f);
			CheckNeighbor(checkPos, brightness, range);
			//Check down 
			checkPos = new Vector2(curPos.x, curPos.y - 1f);
			CheckNeighbor(checkPos, brightness, range);
			//Check left 
			checkPos = new Vector2(curPos.x - 1f, curPos.y);
			CheckNeighbor(checkPos, brightness, range);
			//Check right
			checkPos = new Vector2(curPos.x + 1f, curPos.y);
			CheckNeighbor(checkPos, brightness, range);
			//Check Up Right corner
			checkPos = new Vector2(curPos.x + 1f, curPos.y + 1f);
			CheckNeighbor(checkPos, brightness, range);
			//Check Up Left corner
			checkPos = new Vector2(curPos.x - 1f, curPos.y + 1f);
			CheckNeighbor(checkPos, brightness, range);
			//Check Bottom Left corner
			checkPos = new Vector2(curPos.x - 1f, curPos.y - 1f);
			CheckNeighbor(checkPos, brightness, range);
			//Check Bottom Right corner
			checkPos = new Vector2(curPos.x + 1f, curPos.y - 1f);
			CheckNeighbor(checkPos, brightness, range);
			yield return new WaitForSeconds(0.1f);
			transmitting = false;

		}

		private void CheckNeighbor(Vector2 tileNextPos, float brightness, int range)
		{
			if (tileManager.lightTiles.ContainsKey(tileNextPos)) {
				tileManager.lightTiles[tileNextPos].InjectLight(brightness, range);
			}
		}
	}
}