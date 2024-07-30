
namespace Weapons.WeaponAttachments
{
	public class Flashlight : WeaponAttachment
	{
		protected void Awake()
		{
			InteractionKey = "Remove Flashlight";
			AttachmentType = AttachmentType.Flashlight;
			AttachmentSlot = AttachmentSlot.Flashlight;
		}

		public override void AttachBehaviour(InventoryApply interaction, Gun gun)
		{
			//TODO: itemActionButton for flashlight?
		}
		
		public override void DetachBehaviour(ContextMenuApply interaction, Gun gun)
		{
			//TODO: itemActionButton for flashlight?
		}
	}
}