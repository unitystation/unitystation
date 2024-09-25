
namespace Weapons.WeaponAttachments
{
	public class Flashlight : WeaponAttachment
	{
		protected void Awake()
		{
			InteractionKey = "Remove Flashlight";
			AttachmentType = AttachmentType.Flashlight;
		}

		public override void AttachBehaviour(Gun gun)
		{
			//TODO: itemActionButton for flashlight?
		}
		
		public override void DetachBehaviour(Gun gun)
		{
			//TODO: itemActionButton for flashlight?
		}
	}
}