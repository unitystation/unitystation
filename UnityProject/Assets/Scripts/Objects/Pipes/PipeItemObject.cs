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

			int offset = PipeFunctions.GetOffsetAngle(transform.localEulerAngles.z);
			Quaternion? rotation = Quaternion.Euler(0.0f, 0.0f, offset);
			var spawn = Spawn.ServerPrefab(pipe.gameObject,registerItem.WorldPositionServer, localRotation: rotation);

			spawn.GameObject.GetComponent<MonoPipe>().SetColour(Colour);
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
