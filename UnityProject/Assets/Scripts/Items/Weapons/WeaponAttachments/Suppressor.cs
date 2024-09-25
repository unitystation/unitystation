
namespace Weapons.WeaponAttachments
{
	public class Suppressor : WeaponAttachment
	{
		protected void Awake()
		{
			InteractionKey = "Remove Suppressor";
			AttachmentType = AttachmentType.Suppressor;
		}

		public override bool AttachCheck(Gun gun)
		{
			return !gun.IsSuppressed;
		}
		
		public override bool DetachCheck(Gun gun)
		{
			return gun.IsSuppressed;
		}
		
		public override void AttachBehaviour(Gun gun)
		{
			gun.SyncIsSuppressed(gun.IsSuppressed, true);
		}
		
		public override void DetachBehaviour(Gun gun)
		{
			gun.SyncIsSuppressed(gun.IsSuppressed, false);
		}
	}
}