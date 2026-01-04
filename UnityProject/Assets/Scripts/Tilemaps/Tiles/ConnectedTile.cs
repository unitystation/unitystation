using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

namespace Tiles
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
