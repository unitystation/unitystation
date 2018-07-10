using System;
using Tilemaps.Behaviours.Meta.Data;
using UI;

namespace Tilemaps.Behaviours.Meta
{
	[Serializable]
	public class MetaDataNode
	{
		public static readonly MetaDataNode None = new MetaDataNode() {Room = 0};

		public AtmosValues Atmos { get; } = new AtmosValues();

		public int Room;

		public bool IsSpace => Room < 0;
		public bool IsRoom => Room > 0;
		public bool Exists => this != None;
	}
}