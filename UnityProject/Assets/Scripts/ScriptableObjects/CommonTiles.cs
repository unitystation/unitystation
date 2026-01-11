using System.Collections;
using System.Collections.Generic;
using ScriptableObjects;
using Tiles;
using UnityEngine;

[CreateAssetMenu(fileName = "CommonTiles", menuName = "Singleton/CommonTiles")]
public class CommonTiles : SingletonScriptableObject<CommonTiles>
{
	public OverlayTile IceEffect;
	public OverlayTile PowderSalt;
	public OverlayTile Powder;
	public OverlayTile Liquid;
	public OverlayTile LiquidBig;
}
