using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Pipes
{
	public class ReservoirTank : MonoPipe
	{
		private void Start()
		{
			pipeData.PipeAction = new ReservoirAction();
			base.Start();
		}

	}

}
