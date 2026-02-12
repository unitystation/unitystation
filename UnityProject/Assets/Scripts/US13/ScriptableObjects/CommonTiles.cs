using UnityEngine;
using US13.Tilemaps.Tiles;

namespace US13.ScriptableObjects
{
	[CreateAssetMenu(fileName = "CommonTiles", menuName = "Singleton/CommonTiles")]
	public class CommonTiles : SingletonScriptableObject<CommonTiles>
	{
		public OverlayTile IceEffect;
		public OverlayTile PowderSalt;
		public OverlayTile Powder;
		public OverlayTile Liquid;
		public OverlayTile LiquidBig;
	}
}
