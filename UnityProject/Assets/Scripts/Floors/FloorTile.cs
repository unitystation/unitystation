using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class FloorTile : MonoBehaviour
{

    public GameObject fireScorch;
    public GameObject ambientTile;

    public void AddFireScorch()
    {
        if (fireScorch == null)
        {
            //Do poolspawn here
            fireScorch = EffectsFactory.Instance.SpawnScorchMarks(transform);
        }
    }

    void Start()
    {
        CheckAmbientTile();
    }

    public void CheckAmbientTile()
    {
        if (ambientTile == null)
        {
            ambientTile = GameObject.Instantiate(Resources.Load("AmbientTile") as GameObject, transform.position, Quaternion.identity, transform);
        }
    }

    public void CleanTile()
    {
        if (fireScorch != null)
        {
            fireScorch.transform.parent = null;
            PoolManager.Instance.PoolClientDestroy(fireScorch);
        }
    }
}


