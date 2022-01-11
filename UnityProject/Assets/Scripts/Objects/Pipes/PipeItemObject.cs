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

			var monoPipe = spawn.GameObject.GetComponent<MonoPipe>();

			monoPipe.SetColour(Colour);

			if (spawn.GameObject.TryGetComponent<Directional>(out var directional))
			{
				var orientation = Orientation.GetOrientation(transform.localEulerAngles.z);

//TODO: find the cause for up and down being swaped and remove this hacky fix!
				if (orientation == Orientation.Up)
				{
					orientation = Orientation.Down;
				}
				else if (orientation == Orientation.Down)
				{
					orientation = Orientation.Up;
				}

				directional.FaceDirection(orientation);
			}

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
