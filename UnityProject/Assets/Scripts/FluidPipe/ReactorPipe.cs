using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pipes
{
	public class ReactorPipe : MonoPipe
	{
		public List<ReactorPipe> ConnectedCores = new List<ReactorPipe>(); //needs To check properly
		public void Start()
		{
			pipeData.PipeAction = new ReservoirAction();
			base.Start();
		}

		public override void TickUpdate()
		{

		}
	}
}

