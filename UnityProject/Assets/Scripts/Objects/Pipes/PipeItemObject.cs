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
				var New = Spawn.ServerPrefab(Pipe.gameObject,registerItem.WorldPositionServer, localRotation: rot );
				New.GameObject.GetComponent<MonoPipe>().SetColour(Colour);
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

		public override Connections GetConnections()
		{
			if (pipeObject != null)
			{
				return (pipeObject.pipeData.Connections.Copy());
			}
			return null;
		}
	}
}
