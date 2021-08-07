using System.Collections.Generic;
using UnityEngine;
using Systems.Pipes;
using Objects.Atmospherics;


namespace Items.Atmospherics
{
	public class PipeItemObject : PipeItem
	{
		public MonoPipe pipeObject;

		public override void BuildPipe()
		{
			var pipe = GetPipeObject();
			if (pipe == null) return;

			int Offset = PipeFunctions.GetOffsetAngle(transform.localEulerAngles.z);
			Quaternion? rot = Quaternion.Euler(0.0f, 0.0f,Offset );
			var New = Spawn.ServerPrefab(pipe.gameObject,registerItem.WorldPositionServer, localRotation: rot );
			New.GameObject.GetComponent<MonoPipe>().SetColour(Colour);
			_ = Despawn.ServerSingle(gameObject);
		}

		public virtual void Setsprite() { }

		public virtual MonoPipe GetPipeObject()
		{
			return pipeObject;
		}

		public override Connections GetConnections()
		{
			if (pipeObject != null)
			{
				return pipeObject.pipeData.Connections.Copy();
			}

			return null;
		}
	}
}
