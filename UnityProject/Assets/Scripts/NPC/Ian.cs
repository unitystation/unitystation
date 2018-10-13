using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using PathFinding;

public class Ian : MonoBehaviour
{
	private PathFinder pathFinder;
	private CustomNetTransform customNetTransform;

	private void Awake()
	{
		pathFinder = GetComponent<PathFinder>();
		customNetTransform = GetComponent<CustomNetTransform>();
	}

}
