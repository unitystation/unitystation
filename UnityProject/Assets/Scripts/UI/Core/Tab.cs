using UnityEngine;

public class Tab : MonoBehaviour {
	[HideInInspector]
	public bool Hidden = false;
	public bool isPopOut = true;

	public virtual void RefreshTab() { }
}