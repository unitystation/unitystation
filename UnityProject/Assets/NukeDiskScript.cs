using System;
using System.Collections;
using System.Collections.Generic;
using Atmospherics;
using UnityEngine;
using Mirror;
using UnityEngine.Events;
using UnityEngine.Serialization;
public class NukeDiskScript : NetworkBehaviour
{
	public Sprite[] spriteList;
	public SpriteRenderer spriteRenderer;
    // Start is called before the first frame update
    void Start()
    {
		StartCoroutine(Animation());
    }

    // Update is called once per frame
    void Update()
    {
		
    }
	private IEnumerator Animation()
	{
		int i = 0;
		while (true)
		{
			if(i >= spriteList.Length) { i = 0; }
			spriteRenderer.sprite = spriteList[i];
			yield return WaitFor.Seconds(0.2f);
			i++;
		}
	}
}
