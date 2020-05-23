namespace Spells
{
	public class MimeBox : Spell
	{
		protected override string FormatInvocationMessage(ConnectedPlayer caster, string modPrefix)
		{
			return modPrefix + string.Format(SpellData.InvocationMessage, caster.Script.characterSettings.PossessivePronoun());
		}

		public override bool ValidateCast(ConnectedPlayer caster)
		{
			if (!base.ValidateCast(caster))
			{
				return false;
			}

			if (!caster.Script.mind.IsMiming)
			{
				Chat.AddExamineMsg(caster.GameObject, "You must dedicate yourself to silence first!");
				return false;
			}

			return true;
		}

		public override bool CastSpellServer(ConnectedPlayer caster)
		{
			var box = Spawn.ServerPrefab("mime_box").GameObject;
			Inventory.ServerAdd(box, caster.Script.ItemStorage.GetActiveHandSlot(), ReplacementStrategy.DropOther);

			return true;
		}
	}
}
