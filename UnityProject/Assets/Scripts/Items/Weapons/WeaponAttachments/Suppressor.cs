
namespace Weapons.WeaponAttachments
{
	public class Suppressor : WeaponAttachment
	{
		protected void Awake()
		{
			InteractionKey = "Remove Suppressor";
			AttachmentType = AttachmentType.Suppressor;
			AttachmentSlot = AttachmentSlot.Suppressor;
		}

		public override bool AttachCheck(Gun gun)
		{
			return !gun.isSuppressed;
		}
		
		public override bool DetachCheck(Gun gun)
		{
			return gun.isSuppressed;
		}
		
		public override void AttachBehaviour(InventoryApply interaction, Gun gun)
		{
			gun.SyncIsSuppressed(gun.isSuppressed, true);
		}
		
		public override void DetachBehaviour(ContextMenuApply interaction, Gun gun)
		{
			gun.SyncIsSuppressed(gun.isSuppressed, false);
		}
	}
}