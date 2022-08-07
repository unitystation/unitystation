using System;
using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;

namespace UI
{
	[Serializable]
	public class MaterialNetLabel : NetText_label
	{
		//Will allow the material label to be updated after the NetTab is opened.
		public override void AfterInit()
		{
			//Logger.Log("MaterialNetLabel: Updating " + Value);
			//UpdatePeepers();
		}
	}
}
