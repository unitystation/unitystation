using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spray
{
    /// <summary>
    /// Used to call a spray coming from the player, where position is the target, bottle is the ReagentContainer of the item,
    /// spray is the prefab to spray, and customCol and col are for if you want the spray to have custom colours.
    /// </summary>
    /// <param name="bottle"></param>
    /// <param name="position"></param>
    /// <param name="spray"></param>
    /// <param name="customCol"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public static IEnumerator TriggerSpray(ReagentContainer bottle, Vector3 position, GameObject spray, bool customCol = false, Color col = new Color())
    {
        //Work out direction from player for spray transformations
        int xDir = -(PlayerManager.PlayerScript.WorldPos.x - position.CutToInt().x);
        int yDir = -(PlayerManager.PlayerScript.WorldPos.y - position.CutToInt().y);

        //Create spray game objects
        GameObject SprayGO = PoolManager.PoolNetworkInstantiate(spray, PlayerManager.PlayerScript.WorldPos);
        GameObject SprayGO2;
        GameObject SprayGO3;
        if (Mathf.Abs(xDir) == Mathf.Abs(yDir))
        {
            SprayGO2 = PoolManager.PoolNetworkInstantiate(spray, PlayerManager.PlayerScript.WorldPos + new Vector3(-yDir, xDir));
            SprayGO3 = PoolManager.PoolNetworkInstantiate(spray, PlayerManager.PlayerScript.WorldPos + new Vector3(yDir, -xDir));
        }
        else
        {
            SprayGO2 = PoolManager.PoolNetworkInstantiate(spray, PlayerManager.PlayerScript.WorldPos + new Vector3(yDir, xDir));
            SprayGO3 = PoolManager.PoolNetworkInstantiate(spray, PlayerManager.PlayerScript.WorldPos + new Vector3(-yDir, -xDir));
        }
        SprayGO2.SetActive(false);
        SprayGO3.SetActive(false);

        bool canSpray = false;
        string effect = "";

        //Search for chemicals in container to determine effects of spray
        foreach (KeyValuePair<string, float> Chemical in bottle.Contents)
        {
            if (Chemical.Key == "cleaner")
            {
                if (Chemical.Value >= 5f)
                {
                    canSpray = true;
                    effect = "cleaning";
                }
            }
            else if(Chemical.Key == "water")
            {
                if (Chemical.Value >= 5f)
                {
                    canSpray = true;
                    effect = "extinguish";
                }
            }
            else
            {
                if (Chemical.Value >= 5f) //Still sprays if there are more than 5 of any liquid in container
                {
                    canSpray = true;
                }
            }
        }
        if (canSpray)
        {
            Transform spraySprite = SprayGO.transform.GetChild(0);
            Transform spray2Sprite = SprayGO2.transform.GetChild(0);
            Transform spray3Sprite = SprayGO3.transform.GetChild(0);
            //For custom colours
            if (customCol)
            {
                spraySprite.gameObject.GetComponent<SpriteRenderer>().color = col;
                spray2Sprite.gameObject.GetComponent<SpriteRenderer>().color = col;
                spray3Sprite.gameObject.GetComponent<SpriteRenderer>().color = col;
            }
            //For directions of spray
            if (xDir > 0)
            {
                spraySprite.Rotate(new Vector3(0, 0, 180));
                spray2Sprite.Rotate(new Vector3(0, 0, 180));
                spray3Sprite.Rotate(new Vector3(0, 0, 180));
            }
            else if (xDir == 0)
            {
                spraySprite.Rotate(new Vector3(0, 0, 90 * -yDir));
                spray2Sprite.Rotate(new Vector3(0, 0, 90 * -yDir));
                spray3Sprite.Rotate(new Vector3(0, 0, 90 * -yDir));
            }
            //Cleaning spray
            if (effect == "cleaning")
            {
                for (int j = 0; j < 3; j++)
                {
                    //Clean and move if no wall
                    CleanTile(position + new Vector3(j * xDir, j * yDir));
                    if (!(MatrixManager.GetMetaDataAt(SprayGO.transform.position.CutToInt() + new Vector3(xDir, yDir).CutToInt()).IsOccupied))
                    {
                        SprayGO.transform.Translate(new Vector3(xDir, yDir));
                    }
                    yield return new WaitForSecondsRealtime(0.5f);
                }
            }
            //Extinguishing spray
            else if (effect == "extinguish")
            {
                SprayGO2.SetActive(true);
                SprayGO3.SetActive(true);
                for (int j = 0; j < 3; j++)
                {
                    //Put out fire and move if no walls
                    ExtinguishTile(position + new Vector3(j * xDir, j * yDir));
                    ExtinguishTile(position + new Vector3((j * xDir) - yDir, (j * yDir) + xDir));
                    ExtinguishTile(position + new Vector3((j * xDir) + yDir, (j * yDir) - xDir));
                    if(!(MatrixManager.GetMetaDataAt(SprayGO.transform.position.CutToInt() + new Vector3(xDir, yDir).CutToInt()).IsOccupied))
                    {
                        SprayGO.transform.Translate(new Vector3(xDir, yDir));
                    }
                    if (!(MatrixManager.GetMetaDataAt(SprayGO2.transform.position.CutToInt() + new Vector3(xDir, yDir).CutToInt()).IsOccupied))
                    {
                        SprayGO2.transform.Translate(new Vector3(xDir, yDir));
                    }
                    if (!(MatrixManager.GetMetaDataAt(SprayGO3.transform.position.CutToInt() + new Vector3(xDir, yDir).CutToInt()).IsOccupied))
                    {
                        SprayGO3.transform.Translate(new Vector3(xDir, yDir));
                    }
                    yield return new WaitForSecondsRealtime(0.5f);
                }
            }
            else
            {
                for (int j = 0; j < 3; j++)
                {
                    //Move if no walls
                    if (!(MatrixManager.GetMetaDataAt(SprayGO.transform.position.CutToInt() + new Vector3(xDir, yDir).CutToInt()).IsOccupied))
                    {
                        SprayGO.transform.Translate(new Vector3(xDir, yDir));
                    }
                    SprayGO.transform.Translate(new Vector3(xDir, yDir));
                    yield return new WaitForSecondsRealtime(0.5f);
                }
            }
            //Destroy spray objects after 3 moves
            GameObject.Destroy(SprayGO);
            GameObject.Destroy(SprayGO2);
            GameObject.Destroy(SprayGO3);
        }
    }

    //Coppied from mop code, cleans decals off of tiles
    public static void CleanTile(Vector3 worldPos)
    {
        var worldPosInt = worldPos.CutToInt();
        var matrix = MatrixManager.AtPoint(worldPosInt);
        var localPosInt = MatrixManager.WorldToLocalInt(worldPosInt, matrix);
        var floorDecals = MatrixManager.GetAt<FloorDecal>(worldPosInt);

        for (var i = 0; i < floorDecals.Count; i++)
        {
            floorDecals[i].DisappearFromWorldServer();
        }
    }

    //Removes all plasma from tiles, in effect putting out fires
    public static void ExtinguishTile(Vector3 worldPos)
    {
        var worldPosInt = worldPos.CutToInt();
        var matrix = MatrixManager.AtPoint(worldPosInt);
        var localPosInt = MatrixManager.WorldToLocalInt(worldPosInt, matrix);
        MetaDataNode node = MatrixManager.GetMetaDataAt(worldPosInt);
        node.GasMix.RemoveGas(Atmospherics.Gas.Plasma, node.GasMix.GetMoles(Atmospherics.Gas.Plasma));
    }
}
