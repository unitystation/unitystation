using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/// <summary>
/// Dynamic list entry
/// </summary>
public class DynamicEntry : MonoBehaviour {
	public List<NetUIElement> Elements => GetComponentsInChildren<NetUIElement>(false).ToList();

//	public virtual void InitSpecial() {}
}