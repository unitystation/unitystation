using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ItemMatrixSystemBehaviour : ItemMatrixSystemInit, IUpdateAt
{
	public virtual SystemType SubsystemType => SystemType.None;

	public virtual void UpdateAt(Vector3Int localPosition) { }

	//TODO If using
}
