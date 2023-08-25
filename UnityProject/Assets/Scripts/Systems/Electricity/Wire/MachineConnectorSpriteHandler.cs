using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Systems.Electricity;
using NaughtyAttributes;
using Core.Editor.Attributes;

namespace Objects.Electrical
{
	public class MachineConnectorSpriteHandler : MonoBehaviour, IServerSpawn
	{
		[SerializeField ]
		public List<PowerTypeCategory> connectables = new List<PowerTypeCategory>();

		[SerializeField, BoxGroup("Sprite Handlers")]
		private SpriteHandler spriteHandlerNorth;

		[SerializeField, BoxGroup("Sprite Handlers")]
		private SpriteHandler spriteHandlerSouth;

		[SerializeField, BoxGroup("Sprite Handlers")]
		private SpriteHandler spriteHandlerEast;

		[SerializeField, BoxGroup("Sprite Handlers")]
		private SpriteHandler spriteHandlerWest;

		[SerializeField]
		public WireConnect Wire;

		private HashSet<PowerTypeCategory> connectionTypes;
		private Dictionary<OrientationEnum, SpriteHandler> spriteHandlers;

		private void Awake()
		{
			connectionTypes = new HashSet<PowerTypeCategory>(connectables);

			spriteHandlers = new Dictionary<OrientationEnum, SpriteHandler>()
			{
				{ OrientationEnum.Up_By0, spriteHandlerNorth },
				{ OrientationEnum.Down_By180, spriteHandlerSouth },
				{ OrientationEnum.Left_By90, spriteHandlerWest },
				{ OrientationEnum.Right_By270, spriteHandlerEast },
			};
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			RefreshSprites();
		}

		public void RefreshSprites()
		{
			HashSet<IntrinsicElectronicData> connections = new HashSet<IntrinsicElectronicData>();
			ElectricityFunctions.SwitchCaseConnections(
					Wire.transform.localPosition, Wire.Matrix, connectionTypes,
					Connection.MachineConnect, Wire.InData, connections);

			HashSet<OrientationEnum> activeDirections = new HashSet<OrientationEnum>();
			foreach (IntrinsicElectronicData connection in connections)
			{
				Vector3 vector = (connection.Present.transform.localPosition - Wire.transform.localPosition).CutToInt();
				activeDirections.Add(Orientation.From(vector).AsEnum());
			}

			foreach (var kvp in spriteHandlers)
			{
				SpriteHandler spriteHandler = kvp.Value;
				if (activeDirections.Contains(kvp.Key))
				{
					if (spriteHandler.CurrentSpriteIndex == -1)
					{
						spriteHandler.ChangeSprite(0);
					}
					else
					{
						spriteHandler.PushTexture();
					}
				}
				else
				{
					spriteHandler.PushClear();
				}
			}
		}
	}
}
