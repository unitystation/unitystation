using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Systems.Pipes;
using Objects;
using Objects.Atmospherics;


namespace Items.Atmospherics
{
	public class PipeItem : NetworkBehaviour, ICheckedInteractable<HandApply>, ICheckedInteractable<HandActivate>
	{
		public Color Colour = Color.white;

		public SpriteHandler SpriteHandler;
		public RegisterItem registerItem;
		private PlayerRotatable rotatable;

		private void Awake()
		{
			SpriteHandler = this.GetComponentInChildren<SpriteHandler>();
			registerItem = this.GetComponent<RegisterItem>();
			rotatable = GetComponent<PlayerRotatable>();
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

		#region Interactions

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
				var metaDataNode = registerItem.Matrix.MetaDataLayer.Get(registerItem.LocalPositionServer);
				var connections = GetConnections();
				int offset = PipeFunctions.GetOffsetAngle(transform.localEulerAngles.z);
				connections.Rotate(offset);
				if (PipeTile.CanAddPipe(metaDataNode, connections) == false)
				{
					return;
				}
				ToolUtils.ServerPlayToolSound(interaction);
				BuildPipe();
			}
			else
			{
				rotatable.Rotate();
			}
		}

		public virtual bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public virtual void ServerPerformInteraction(HandActivate interaction)
		{
			rotatable.Rotate();
		}

		#endregion Interactions

		public virtual void BuildPipe()
		{
		}

		public virtual Connections GetConnections()
		{
			return null;
		}
	}
}
