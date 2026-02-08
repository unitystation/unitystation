using UnityEngine;
using US13.Core.Lifecycle;
using US13.Items.Pipes;
using US13.Systems.Fluids;

namespace US13.Objects.Pipes
{
	public class PipeItemObject : PipeItem
	{
		public MonoPipe pipeObject;

		public override void BuildPipe()
		{
			var pipe = GetPipeObject();
			if (pipe == null) return;

			var spawn = Spawn.ServerPrefab(pipe.gameObject, registerItem.WorldPositionServer, localRotation: this.rotatable.ByDegreesToQuaternion(this.rotatable.CurrentDirection, Quaternion.identity));

			var monoPipe = spawn.GameObject.GetComponent<MonoPipe>();





			monoPipe.SetColour(Colour);
			monoPipe.directional.FaceDirection(this.rotatable.CurrentDirection);
			monoPipe.SetUpPipes();


			_ = Despawn.ServerSingle(gameObject);
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
				return pipeObject.pipeData.RotatedConnections.Copy();
			}

			return null;
		}
	}
}