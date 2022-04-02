using System;
using Managers;

namespace Communications
{
	public class RadioSignalCommunicator : SignalEmitter
	{

		protected RadioSignalProcessor processor;

		private void Awake()
		{
			processor = GetComponent<RadioSignalProcessor>();
		}

		protected override bool SendSignalLogic()
		{
			//the most basic thing the base can check for now
			//other servers should override this if they want more fine control over the requirements of a successful TrySendSignal()
			if (processor.requiresPower && processor.isPowered == false) return false;
			return true;
		}

		public override void SignalFailed()
		{
			throw new System.NotImplementedException();
		}
	}
}