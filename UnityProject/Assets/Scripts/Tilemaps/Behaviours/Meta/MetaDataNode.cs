using System;

namespace Tilemaps.Behaviours.Meta
{
	
	[Serializable]
	public class MetaDataNode
	{
		private int room = 0;

		public int Room
		{
			get { return room; }
			set { room = value; }
		}

		public void Reset()
		{
			Room = 0;
		}

		public bool IsSpace()
		{
			return Room < 0;
		}
	}
}