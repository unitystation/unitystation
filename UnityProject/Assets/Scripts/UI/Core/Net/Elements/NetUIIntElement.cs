using System;
using System.Text;
using UI.Core.NetUI;

namespace UI.Core.Net.Elements
{
	public class NetUIIntElement : NetUIElement<int>
	{
		public override byte[] BinaryValue {
			get => BitConverter.GetBytes(Value);
			set => Value = BitConverter.ToInt32(value);
		}
	}
}
