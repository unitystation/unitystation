namespace Spells
{
	public class MimeWall : Spell
	{
		protected override string FormatInvocationMessage(ConnectedPlayer caster, string modPrefix)
		{
			return string.Format(SpellData.InvocationMessage, caster.Name, caster.CharacterSettings.ThemPronoun());
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
	}
}