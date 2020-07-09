using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Lava Land Random Cave Generator, modified version from this: https://www.youtube.com/watch?v=xNqqfABXTNQ, https://www.dropbox.com/s/qggbs7hnapj6136/ProceduralTilemaps.zip?dl=0
/// </summary>
public class LavaLandRandomGenScript : MonoBehaviour
{

    public int iniChance;

    public int birthLimit;

    public int deathLimit;

    public int numR;

    private int count = 0;

    private int[,] terrainMap;
    public Vector3Int tmpSize;
    public Tilemap topMap;
    //public Tilemap botMap;
    public TileBase topTile;
    //public AnimatedTile botTile;

    public LayerTile wallTile;

    int width;
    int height;

    private TileChangeManager tileChangeManager;

    private void Start()
    {
	    LavaLandManager.Instance.randomGenScripts.Add(this);


	    tileChangeManager = transform.parent.parent.parent.GetComponent<TileChangeManager>();
    }

    public void DoSim()
    {
	    var gameObjectPos = gameObject.transform.localPosition.RoundToInt();
	    width = tmpSize.x;
	    height = tmpSize.y;

	    if (terrainMap==null)
	    {
		    terrainMap = new int[width, height];
		    InitPos();
	    }


	    for (int i = 0; i < numR; i++)
	    {
		    terrainMap = GenTilePos(terrainMap);
	    }

	    for (int x = 0; x < width; x++)
	    {
		    for (int y = 0; y < height; y++)
		    {
			    if (terrainMap[x, y] != 1)
			    {
				    var pos = new Vector3Int(-x + width / 2, -y + height / 2, 0) + gameObjectPos;
				    tileChangeManager.UpdateTile(pos, wallTile);
				    //botMap.SetTile(new Vector3Int(-x + width / 2, -y + height / 2, 0), botTile);
			    }
		    }
	    }
    }

    public void InitPos()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                terrainMap[x, y] = Random.Range(1, 101) < iniChance ? 1 : 0;
            }
        }
    }


    public int[,] GenTilePos(int[,] oldMap)
    {
        int[,] newMap = new int[width,height];
        int neighb;
        BoundsInt myB = new BoundsInt(-1, -1, 0, 3, 3, 1);


        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                neighb = 0;
                foreach (var b in myB.allPositionsWithin)
                {
                    if (b.x == 0 && b.y == 0) continue;
                    if (x+b.x >= 0 && x+b.x < width && y+b.y >= 0 && y+b.y < height)
                    {
                        neighb += oldMap[x + b.x, y + b.y];
                    }
                    else
                    {
                        neighb++;
                    }
                }

                if (oldMap[x,y] == 1)
                {
                    if (neighb < deathLimit) newMap[x, y] = 0;

                        else
                        {
                            newMap[x, y] = 1;

                        }
                }

                if (oldMap[x,y] == 0)
                {
                    if (neighb > birthLimit) newMap[x, y] = 1;

					else
					{
						newMap[x, y] = 0;
					}
                }
            }
        }

        return newMap;
    }

    public void ClearMap(bool complete)
    {

		topMap.ClearAllTiles();
	    //botMap.ClearAllTiles();
	    if (complete)
	    {
		    terrainMap = null;
	    }
    }
}
