using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

namespace Pipes
{
	public class PipeItem : NetworkBehaviour, ICheckedInteractable<HandApply>
	{
		public Color Colour = Color.white;

		[SyncVar(hook = nameof(SetRotation))]
		public float Netz = 0;


		public SpriteHandler SpriteHandler;
		public RegisterItem registerItem;

		private void Awake()
		{
			SpriteHandler = this.GetComponentInChildren<SpriteHandler>();
			registerItem = this.GetComponent<RegisterItem>();
		}

		public void SetRotation(float Oldz, float Newz)
		{
			Netz = Newz;
			transform.eulerAngles =  new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, Newz);
		}

		public void Start()
		{
			SpriteHandler.SetColor(Colour);
		}

		public void SetColour(Color newColour)
		{
			Colour = newColour;
			SpriteHandler.SetColor(Colour);
		}

		public virtual bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (interaction.TargetObject != gameObject) return false;
			if (interaction.HandObject == null) return false;
			return true;
		}

		public virtual void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Wrench))
			{
				var ZeroedLocation = new Vector3Int(x:registerItem.LocalPosition.x, y:registerItem.LocalPosition.y,0);
				var metaData = registerItem.Matrix.MetaDataLayer.Get(ZeroedLocation);
				var thisConnections = GetConnections();
				int Offset = PipeFunctions.GetOffsetAngle(transform.localEulerAngles.z);
				thisConnections.Rotate(Offset);

				foreach (var Pipeo in metaData.PipeData)
				{
					var TheConnection = Pipeo.pipeData.Connections;
					for (int i = 0; i < thisConnections.Directions.Length; i++)
					{
						if (thisConnections.Directions[i].Bool && TheConnection.Directions[i].Bool)
						{
							return;
						}
					}
				}
				BuildPipe();
			}

			this.transform.Rotate(0, 0, -90);
			SetRotation(Netz,transform.eulerAngles.z);
		}

		public virtual void BuildPipe()
		{
		}

		public virtual Connections GetConnections()
		{
			return (null);
		}
	}
}

