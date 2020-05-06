using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class CustomNetSceneChecker : MonoBehaviour
{
	/// <summary>
	/// If this gameobject has moved between scenes you need to call this
	/// method to rebuild the observers. Only call it once. So if you are in
	/// a shuttle that has crossed into a subscene then call it on the parent
	/// of that shuttle tree.
	/// </summary>
	public void ResetObserversOnSceneChange()
	{
		foreach(var n in NetworkIdentity.spawned)
		{
			n.Value.RebuildObservers(false);
		}
	}
}
