using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DynamicEntry : MonoBehaviour {
	public List<NetUIElement> Elements => GetComponentsInChildren<NetUIElement>(true).ToList();
}