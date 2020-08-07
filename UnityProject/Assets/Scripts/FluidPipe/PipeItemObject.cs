using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pipes
{
	public class PipeItemObject : PipeItem
	{
		public MonoPipe pipeObject;

		public override void BuildPipe()
		{
			var searchVec = registerItem.LocalPosition;
			var Pipe = (GetPipeObject());
			if (Pipe != null)
			{
				int Offset = PipeFunctions.GetOffsetAngle(transform.localEulerAngles.z);
				Quaternion? rot = Quaternion.Euler(0.0f, 0.0f,Offset );
				Spawn.ServerPrefab(Pipe.gameObject,registerItem.WorldPositionServer, localRotation: rot );
				Despawn.ServerSingle(this.gameObject);
			}

		}

		public virtual void Setsprite()
		{
		}


		public virtual MonoPipe GetPipeObject()
		{
			return pipeObject;
		}

		public override PipeLayer GetPipeLayer()
		{
			if (pipeObject != null)
			{
				return (pipeObject.pipeData.PipeLayer);
			}
			return PipeLayer.Second;
		}
	}
}