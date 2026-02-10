using System;
using US13.UI.Core.Net.Elements;

namespace US13.UI.Objects.Research.Protolathe
{
	[Serializable]
	public class MaterialNetLabel : NetText_label
	{
		//Will allow the material label to be updated after the NetTab is opened.
		public override void AfterInit()
		{
			//Loggy.Log("MaterialNetLabel: Updating " + Value);
			//UpdatePeepers();
		}
	}
}
