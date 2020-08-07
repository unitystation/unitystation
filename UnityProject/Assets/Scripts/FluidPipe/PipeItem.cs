using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pipes
{
	public class PipeItem : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		public Color Colour;
		//This is to be never rotated on items


		public SpriteHandler SpriteHandler;
		public RegisterItem registerItem;

		private void Awake()
		{
			SpriteHandler = this.GetComponentInChildren<SpriteHandler>();
			registerItem = this.GetComponent<RegisterItem>();
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
				var INLayer = GetPipeLayer();
				if (metaData.PipeData.Any(x => x.pipeData.PipeLayer == INLayer)) return;
				BuildPipe();
				return;
			}

			this.transform.Rotate(0, 0, -90);
		}

		public virtual void BuildPipe()
		{
		}

		public virtual PipeLayer GetPipeLayer()
		{
			return (PipeLayer.Second);
		}
	}
}

