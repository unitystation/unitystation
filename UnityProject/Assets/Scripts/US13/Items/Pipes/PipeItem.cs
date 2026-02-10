using Mirror;
using UnityEngine;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Sprite_Handler;
using US13.Items.Tool;
using US13.Items.Traits;
using US13.Objects.Directionals;
using US13.Objects.Pipes;
using US13.Systems.Fluids;
using US13.Tilemaps.Behaviours.Objects;
using Util;

namespace US13.Items.Pipes
{
	public class PipeItem : NetworkBehaviour, ICheckedInteractable<HandApply>, ICheckedInteractable<HandActivate>
	{
		public Color Colour = Color.white;

		public SpriteHandler SpriteHandler;
		public RegisterItem registerItem;
		public Rotatable rotatable;

		private void Awake()
		{
			SpriteHandler = this.GetComponentInChildren<SpriteHandler>();
			registerItem = this.GetComponent<RegisterItem>();
			rotatable = GetComponent<Rotatable>();
		}

		public void Start()
		{
			if (isServer == false) return;
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
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.TargetObject != gameObject) return false;
			if (interaction.HandObject == null) return false;
			return true;
		}

		public virtual void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Wrench))
			{
				var metaDataNode = registerItem.Matrix.MetaDataLayer.Get(registerItem.LocalPositionServer);
				var connectionsCopy = GetConnections();
				int offset = PipeFunctions.GetOffsetAngle(transform.localEulerAngles.z);
				connectionsCopy.Rotate(offset);
				if (PipeTile.CanAddPipe(metaDataNode, connectionsCopy) == false)
				{
					return;
				}
				ToolUtils.ServerUseToolWithActionMessages(interaction, 0,
						string.Empty,
						string.Empty,
						$"You fasten the {gameObject.ExpensiveName()}.",
						$"{interaction.Performer} fastens the {gameObject.ExpensiveName()}",
						BuildPipe);
			}
			else
			{
				rotatable.RotateBy(1);
			}
		}

		public virtual bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public virtual void ServerPerformInteraction(HandActivate interaction)
		{
			rotatable.RotateBy(1);
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
