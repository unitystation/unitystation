using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameGizmo : MonoBehaviour
{
	public void OnDestroy()
	{
		GameGizmomanager.Instance.ActiveGizmos.Remove(this);
	}
}
