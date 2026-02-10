namespace US13.Tilemaps.Tiles
{
	public enum ConnectCategory
	{
		Walls,
		Windows,
		Tables,
		Floors,
		None
	}

	public enum ConnectType
	{
		ToAll,
		ToSameCategory,
		ToSelf,
		ToCategoryAndSelf,
		WhiteList
	}

	public class ConnectedTile : BasicTile
	{




	}
}
