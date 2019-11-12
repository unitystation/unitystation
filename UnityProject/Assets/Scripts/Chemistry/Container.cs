using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Container : MonoBehaviour
{
	public float Temperature = 293.15f;
	public int MaxCapacity = 100;
	public Dictionary<string, float> Contents = new Dictionary<string, float>();
}
