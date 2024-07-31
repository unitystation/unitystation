using System;
using UnityEngine;

namespace Weapons.WeaponAttachments
{
	public abstract class WeaponAttachment : MonoBehaviour
	{
		[NonSerialized] public string InteractionKey;
		[NonSerialized] public AttachmentType AttachmentType;

		[NonSerialized] public bool AllowDuplicateAttachments = false;

		public virtual bool AttachCheck(Gun gun)
		{
			return true;
		}
		
		public virtual bool DetachCheck(Gun gun)
		{
			return true;
		}
		
		public abstract void AttachBehaviour(Gun gun);
		public abstract void DetachBehaviour(Gun gun);
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

}