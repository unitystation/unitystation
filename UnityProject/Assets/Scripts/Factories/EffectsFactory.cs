using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Sprites;

public class EffectsFactory : NetworkBehaviour
{
    private static EffectsFactory effectsFactory;

    public static EffectsFactory Instance
    {
        get
        {
            if (effectsFactory == null)
            {
                effectsFactory = FindObjectOfType<EffectsFactory>();
                Instance.Init();
            }
            return effectsFactory;
        }
    }

    private GameObject fireTile { get; set; }
    private GameObject scorchMarksTile { get; set; }
    private GameObject shroudTile { get; set; }

    private GameObject bloodTile { get; set; }

    //Parents to make tidy
    private GameObject shroudParent;

    void Init()
    {
        //Do init stuff
        Instance.fireTile = Resources.Load("FireTile") as GameObject;
        Instance.scorchMarksTile = Resources.Load("ScorchMarks") as GameObject;
        Instance.shroudTile = Resources.Load("ShroudTile") as GameObject;
        Instance.bloodTile = Resources.Load("BloodSplat") as GameObject;
        //Parents
        Instance.shroudParent = new GameObject();
        Instance.shroudParent.transform.position += new Vector3(0.5f, 0.5f, 0);
        Instance.shroudParent.name = "FieldOfView(Shrouds)";
    }

    //FileTiles are client side effects only, no need for network sync (triggered by same event on all clients/server)
    public void SpawnFileTile(float fuelAmt, Vector3 position)
    {
        //ClientSide pool spawn
        GameObject fireObj = PoolManager.PoolClientInstantiate(Instance.fireTile, position, Quaternion.identity);
        FireTile fT = fireObj.GetComponent<FireTile>();
        fT.StartFire(fuelAmt);
    }

    public GameObject SpawnScorchMarks(Transform parent)
    {
        //ClientSide spawn
        GameObject sM = PoolManager.PoolClientInstantiate(Instance.scorchMarksTile, parent.position, Quaternion.identity);
        sM.transform.parent = parent;
        return sM;
    }

    public GameObject SpawnShroudTile(Vector3 pos)
    {
        GameObject sT = PoolManager.PoolClientInstantiate(Instance.shroudTile, pos, Quaternion.identity);
        sT.transform.parent = shroudParent.transform;
        return sT;
    }

    [Server]
    public void BloodSplat(Vector3 pos, BloodSplatSize splatSize)
    {
        GameObject b = PoolManager.PoolNetworkInstantiate(Instance.bloodTile, pos, Quaternion.identity);
        BloodSplat bSplat = b.GetComponent<BloodSplat>();
        //choose a random blood sprite
        int spriteNum = 0;
        switch (splatSize)
        {
            case BloodSplatSize.small:
                spriteNum = Random.Range(137, 139);
                break;
            case BloodSplatSize.medium:
                spriteNum = Random.Range(116, 120);
                break;
            case BloodSplatSize.large:
                spriteNum = Random.Range(51, 56);
                break;
        }

        bSplat.bloodSprite = spriteNum;
    }
}