using System;
using UnityEngine;

[Serializable]
public class XYCoord
{
    [SerializeField] private int x;
    [SerializeField] private int y;

    public XYCoord(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}