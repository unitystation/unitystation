using System;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class UniTile : Tile
{
    [SerializeField]
    private Matrix4x4 m_ChildTransform = Matrix4x4.identity;

    /// <summary>
    ///   <para>Transform of child Sprite, if there was one</para>
    /// </summary>
    public Matrix4x4 ChildTransform
    {
        get
        {
            return m_ChildTransform;
        }
        set
        {
            m_ChildTransform = value;
        }
    }

}
