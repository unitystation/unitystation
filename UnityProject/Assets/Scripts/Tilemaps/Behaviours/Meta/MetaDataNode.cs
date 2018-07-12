using System;
using Tilemaps.Behaviours.Meta.Data;
using UI;

namespace Tilemaps.Behaviours.Meta
{
	public enum NodeType
	{
		Space, Room, Wall
	}
	
	[Serializable]
	public class MetaDataNode
	{
		public static readonly MetaDataNode None = new MetaDataNode() {Room = -1};

		public AtmosValues Atmos { get; } = new AtmosValues();

		public int Room;
		
		public NodeType Type;

		public bool IsSpace => Type == NodeType.Space;
		public bool IsRoom => Type == NodeType.Room;
		public bool IsWall => Type == NodeType.Wall;
		
		public bool Exists => this != None;
	}
}