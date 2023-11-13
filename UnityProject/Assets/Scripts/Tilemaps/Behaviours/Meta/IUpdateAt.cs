using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUpdateAt
{
	public SystemType SubsystemType { get; }
    public void UpdateAt(Vector3Int localPosition);
}
