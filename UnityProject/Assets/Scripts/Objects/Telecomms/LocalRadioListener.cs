using System.Collections;
using System.Collections.Generic;
using Communications;
using UnityEngine;

namespace Objects.Telecomms
{
	public class LocalRadioListener : SignalEmitter
	{
		protected override bool SendSignalLogic()
		{
			throw new System.NotImplementedException();
		}

		public override void SignalFailed()
		{
			throw new System.NotImplementedException();
		}
	}

}
