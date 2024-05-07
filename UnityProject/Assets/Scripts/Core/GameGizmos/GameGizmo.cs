using System;
using System.Collections;
using System.Collections.Generic;
using InGameGizmos;
using UnityEngine;

public class GameGizmo : MonoBehaviour
{
	public void OnDestroy()
	{
		GameGizmomanager.Instance.OrNull()?.ActiveGizmos?.Remove(this);
	}
	public void Remove()
	{
		Destroy(this.gameObject);
	}

	public virtual string Serialisable()
	{
		return "";
	}

	public virtual void DeSerialisable(string Data)
	{
	}


}
