using System.Collections;
using System.Collections.Generic;
using UnityEngine;


	public class LightTile : MonoBehaviour
	{
		private LightingTileManager tileManager;
		private bool transmitting;
		public SpriteRenderer thisSprite { get; set; }

		private void Start()
		{
			thisSprite = GetComponentInChildren<SpriteRenderer>();
			tileManager = GetComponentInParent<LightingTileManager>();
		}

		//If this tile touches a light source then absorb the light and send to surrounds
		public void InjectLight(float brightness, int range)
		{
			if (!transmitting)
			{
				transmitting = true;
				ChangeBrightness(brightness);
				StartCoroutine(PassTheLight(brightness, range));
			}
		}

		//Set alpha of tile
		public void ChangeBrightness(float brightness)
		{
			Color tempColor = thisSprite.color;
			if (brightness != 0f)
			{
				float alpha = Mathf.Clamp(tempColor.a - brightness / 100f, 0f, 1f);
				tempColor.a = alpha;
				thisSprite.color = tempColor;
			}
			else
			{
				tempColor.a = 1f;
				thisSprite.color = tempColor;
			}
		}

		//Pass the brightness of the light to neighbor tiles
		private IEnumerator PassTheLight(float _brightness, int range)
		{
			//the range is key 1 = closest
			Dictionary<int, List<Vector2>> radialDispersion = new Dictionary<int, List<Vector2>>();
			//the different ranges for the Dictionary
			for (int i = 1; i <= range; i++)
			{
				//row and column length for the box radial
				int rangeFinder = i + 1 + i;
				//To store the current range box radial tile positions
				List<Vector2> lightTiles = new List<Vector2>();

				//working left to right, for-loop below iterates through the rows
				for (int k = 1; k <= rangeFinder; k++)
				{
					if (k == 1)
					{
						//toprow
						//Starting at top left
						Vector2 tilePos = new Vector2(transform.position.x - i,
							transform.position.y + i);
						for (int tile = 1; tile <= rangeFinder; tile++)
						{
							if (tile == 1)
							{
								lightTiles.Add(tilePos);
							}
							else
							{
								Vector2 nextTile = new Vector2(tilePos.x + tile - 1f, tilePos.y);
								lightTiles.Add(nextTile);
							}
						}
					}
					else if (k == rangeFinder)
					{
						//lastrow
						Vector2 tilePos = new Vector2(transform.position.x - i,
							transform.position.y - i);
						for (int tile = 1; tile <= rangeFinder; tile++)
						{
							if (tile == 1)
							{
								lightTiles.Add(tilePos);
							}
							else
							{
								Vector2 nextTile = new Vector2(tilePos.x + tile - 1f, tilePos.y);
								lightTiles.Add(nextTile);
							}
						}
					}
					else
					{
						//everything else
						Vector2 tilePos = new Vector2(transform.position.x - i,
							transform.position.y + i);
						Vector2 firstTilePos = new Vector2(tilePos.x, tilePos.y - k + 1f);
						lightTiles.Add(firstTilePos);
						Vector2 lastTilePos = new Vector2(tilePos.x + rangeFinder - 1f,
							tilePos.y - k + 1f);
						lightTiles.Add(lastTilePos);
					}
				}
				//End of range list, now add the list to the dictionary at the specific range
				radialDispersion.Add(i, lightTiles);
			}

			yield return WaitFor.EndOfFrame;

			int secondLast = range - 1;
			foreach (KeyValuePair<int, List<Vector2>> tileRadial in radialDispersion)
			{
				//TODO improve light fade off here, at the moment I'm just changing brightness on the last two ranges to fade off
				if (tileRadial.Key == range && _brightness > 0f)
				{
					//Quarter brightness on last range
					foreach (Vector2 tilePos in tileRadial.Value)
					{
						CheckNeighbor(tilePos, 25f);
					}
				}
				else if (tileRadial.Key == secondLast && _brightness > 0f)
				{
					//Half brightness on second last range
					foreach (Vector2 tilePos in tileRadial.Value)
					{
						CheckNeighbor(tilePos, 50f);
					}
				}
				else
				{
					foreach (Vector2 tilePos in tileRadial.Value)
					{
						CheckNeighbor(tilePos, _brightness);
					}
				}
			}
			yield return WaitFor.EndOfFrame;
			transmitting = false;
		}

		private void CheckNeighbor(Vector2 tileNextPos, float brightness)
		{
			if (tileManager.lightTiles.ContainsKey(tileNextPos))
			{
				tileManager.lightTiles[tileNextPos].ChangeBrightness(brightness);
			}
		}
	}
