using System;
using System.Collections.Generic;
using Objects.Engineering;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    private int test1 = 0;
    protected float cow2 = 1f;
    internal List<Vector3> test3 = new List<Vector3>
    {
        new Vector3(0,0,1),
        new Vector3(0,2,4),
        Vector3.one
    };

    public APC apcState;

    void Start()
    {

    }


    // Update is called once per frame
    void Update()
    {

    }

    private void Dig(float cow)
    {
	    cow = Single.NaN;
    }

    public byte killerBytes = 0x44;

    public void TestFunc()
    {
	    var cowainter = 44f;
	    string cowmangler = "big gay balls";
    }
}
