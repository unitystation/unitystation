public static class TileUtils
{
	public static bool IsPassable(params BasicTile[] tile)
	{
		for (var i = 0; i < tile.Length; i++)
		{
			BasicTile t = tile[i];
			if (t && !t.IsPassable())
				return false;
		}

		return true;
	}

	public static bool IsAtmosPassable(params BasicTile[] tile)
	{
		for (var i = 0; i < tile.Length; i++)
		{
			BasicTile t = tile[i];
			if (t && !t.IsAtmosPassable())
				return false;
		}

		return true;
	}

	public static bool IsSpace(params BasicTile[] tile)
	{
		for (var i = 0; i < tile.Length; i++)
		{
			BasicTile t = tile[i];
			if (t && !t.IsSpace())
				return false;
		}

		return true;
	}
}