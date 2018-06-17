using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DynamicEntry : MonoBehaviour {
	public List<NetUIElement> Elements => GetComponentsInChildren<NetUIElement>(false).ToList();

	public virtual void Init() {}
}