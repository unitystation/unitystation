using System;
using Tilemaps.Behaviours.Meta.Data;
using UI;

namespace Tilemaps.Behaviours.Meta
{
	[Serializable]
	public class MetaDataNode
	{
		public static readonly MetaDataNode None = new MetaDataNode() {Room = 0};

		public bool atmosEdge;
		public bool updating;

		public int Room;

		public bool IsSpace => Room < 0;
		public bool IsRoom => Room > 0;
		public bool Exists => this != None;
		
		public float[] AirMix = new float[Gas.Count]; 
		
		public float Temperature;
		public float Moles;
		
		public float Pressure => Moles * Gas.R * Temperature / 2 / 1000;
	}
}