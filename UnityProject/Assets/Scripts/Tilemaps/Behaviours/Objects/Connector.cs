using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;

namespace Pipes
{
	public class Connector : MonoPipe
	{
		private Canister canister;

		public void Start()
		{
			pipeData.PipeAction = new MonoActions();
			base.Start();
		}

		public override void TickUpdate()
		{
			base.TickUpdate();
			if (canister != null)
			{
				pipeData.mixAndVolume.EqualiseWithExternal(canister.container.GasMix);
			}
			pipeData.mixAndVolume.EqualiseWithOutputs(pipeData.Outputs);
		}

		public void DisconnectCanister()
		{
			canister = null;
		}

		public void ConnectCanister(Canister Incanister)
		{
			canister = Incanister;
		}

		public override void OnDisassembly(HandApply interaction)
		{
			if (canister != null)
			{
				canister.Disconnect();
			}
		}
	}
}