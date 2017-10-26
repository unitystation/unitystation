using System;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class UniTileData : ScriptableObject
{
    [SerializeField]
    private string _name;

    [SerializeField]
    private string originalSpriteName;
    
    [SerializeField]
    private string spriteName;

    public string SpriteName
    {
        get { return spriteName; }
        set { spriteName = value; }
    }

    [SerializeField]
    private string spriteSheet;

    [SerializeField]
    private int spriteOffset = -1;

    [SerializeField]
    private Matrix4x4 childTransform = Matrix4x4.identity;

    [SerializeField]
    private Matrix4x4 transform = Matrix4x4.identity;

    [SerializeField]
    private Tile.ColliderType colliderType = Tile.ColliderType.Grid;

    public string Name
    {
        get { return _name; }
        set { _name = value; }
    }

    public string OriginalSpriteName
    {
        get { return originalSpriteName; }
        set { originalSpriteName = value; }
    }

    public string SpriteSheet
    {
        get { return spriteSheet; }
        set { spriteSheet = value; }
    }

    public int SpriteOffset
    {
        get { return spriteOffset; }
        set { spriteOffset = value; }
    }

    public Matrix4x4 ChildTransform
    {
        get { return childTransform; }
        set { childTransform = value; }
    }

    public Matrix4x4 Transform
    {
        get { return transform; }
        set { transform = value; }
    }

    public Tile.ColliderType ColliderType
    {
        get { return colliderType; }
        set { colliderType = value; }
    }
}