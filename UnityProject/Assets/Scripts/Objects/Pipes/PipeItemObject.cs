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

			var quart = Quaternion.Euler(0.0f, 0.0f, transform.localEulerAngles.z);
			var spawn = Spawn.ServerPrefab(pipe.gameObject,registerItem.WorldPositionServer, localRotation: quart);

			if (spawn.GameObject.TryGetComponent<Rotatable>(out var rotatable))
			{
				var orientation = Orientation.GetOrientation(transform.localEulerAngles.z);

				rotatable.FaceDirection(orientation.AsEnum());
			}

			var monoPipe = spawn.GameObject.GetComponent<MonoPipe>();

			monoPipe.SetColour(Colour);
			monoPipe.SetUpPipes();

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
