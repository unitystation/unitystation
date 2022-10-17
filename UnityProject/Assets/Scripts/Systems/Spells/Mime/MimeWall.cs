namespace Systems.Spells
{
	public class MimeWall : Spell
	{
		protected override string FormatInvocationMessage(PlayerInfo caster, string modPrefix)
		{
			return string.Format(SpellData.InvocationMessage, caster.Name, caster.Mind.CurrentCharacterSettings.ThemPronoun(caster.Script));
		}
		public override bool ValidateCast(PlayerInfo caster)
		{
			if (!base.ValidateCast(caster))
			{
				return false;
			}

			if (!caster.Mind.IsMiming)
			{
				Chat.AddExamineMsg(caster.GameObject, "You must dedicate yourself to silence first!");
				return false;
			}

			return true;
		}
	}
}
