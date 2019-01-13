using UnityEngine;

public class Tab : MonoBehaviour {
	[HideInInspector]
	public bool Hidden = false;
	public bool isPopOut = false;

	public virtual void RefreshTab() { }
}