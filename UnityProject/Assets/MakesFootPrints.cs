using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chemistry.Components;

public class MakesFootPrints : MonoBehaviour
{
	public ReagentContainer spillContents;

	public void Awake()
	{
		spillContents = gameObject.GetComponent<ReagentContainer>();
	}

	// Start is called before the first frame update
	void Start()
    {
		spillContents = gameObject.GetComponent<ReagentContainer>();

	}

	// Update is called once per frame
	void Update()
    {
        
    }
}
