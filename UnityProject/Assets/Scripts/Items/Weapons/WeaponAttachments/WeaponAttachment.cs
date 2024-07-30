using System;
using UnityEngine;

namespace Weapons.WeaponAttachments
{
	public abstract class WeaponAttachment : MonoBehaviour
	{
		
		[NonSerialized] public string InteractionKey;
		[NonSerialized] public AttachmentType AttachmentType;
		[NonSerialized] public AttachmentSlot AttachmentSlot;

		public virtual bool AttachCheck(Gun gun)
		{
			return true;
		}
		
		public virtual bool DetachCheck(Gun gun)
		{
			return true;
		}
		
		public abstract void AttachBehaviour(InventoryApply interaction, Gun gun);
		public abstract void DetachBehaviour(ContextMenuApply interaction, Gun gun);
	}
	
	[Flags]
	public enum AttachmentType
	{
		//None exists for Gun allowed attachment selection, do not set the attachment type of a weaponattachment to it.
		None = 0,
		Suppressor = 1 << 0,
		Flashlight = 1 << 1,
		Bayonet = 1 << 2
	}
	
	public enum AttachmentSlot
	{
		Suppressor = 2,
		Flashlight = 3,
		Bayonet = 4,
	}
	
}