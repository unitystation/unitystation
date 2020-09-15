using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestingQuickly : MonoBehaviour
{
	public AddressableReferences.AddressableSprite Reference;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    [NaughtyAttributes.Button("NotValidKey?")]
    public void bob()
    {
	    Reference.NotValidKey();
    }
}
