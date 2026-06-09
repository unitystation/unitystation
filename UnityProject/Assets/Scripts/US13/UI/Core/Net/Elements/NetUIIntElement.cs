using System;

namespace US13.UI.Core.Net.Elements
{
	public class NetUIIntElement : NetUIElement<int>
	{
		public override byte[] BinaryValue {
			get => BitConverter.GetBytes(Value);
			set => Value = BitConverter.ToInt32(value);
		}
	}
}
