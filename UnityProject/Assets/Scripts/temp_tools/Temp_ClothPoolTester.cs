using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Temp_ClothPoolTester : MonoBehaviour
{

    public void SpawnCloth()
    {
        ClothFactory.Instance.CreateCloth("", transform.position);
    }
}
