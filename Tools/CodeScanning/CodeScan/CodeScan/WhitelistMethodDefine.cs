using System.Collections.Generic;

namespace UnitystationLauncher.ContentScanning
{
	public sealed class WhitelistMethodDefine
	{
		public string Name { get; }
		public MType ReturnType { get; }
		public List<MType> ParameterTypes { get; }
		public int GenericParameterCount { get; }

		public WhitelistMethodDefine(
			string name,
			MType returnType,
			List<MType> parameterTypes,
			int genericParameterCount)
		{
			Name = name;
			ReturnType = returnType;
			ParameterTypes = parameterTypes;
			GenericParameterCount = genericParameterCount;
		}
	}

}
