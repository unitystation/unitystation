
using System.Linq;

namespace Spells
{
	public class MimeChair : Spell
	{
		protected override string FormatInvocationMessage(ConnectedPlayer caster, string modPrefix)
		{
			return string.Format(SpellData.InvocationMessage, caster.Name, caster.CharacterSettings.ThemPronoun());
		}

		public override bool CastSpellServer(ConnectedPlayer caster)
		{
			if (!base.CastSpellServer(caster))
			{
				return false;
			}

			var buckleable =
				MatrixManager.GetAt<BuckleInteract>(caster.Script.AssumedWorldPos, true).FirstOrDefault();
			if (buckleable == null)
			{
				return false;
			}

			var directional = buckleable.GetComponent<Directional>();
			if (directional)
			{
				directional.FaceDirection(caster.Script.CurrentDirection);
			}

			buckleable.BucklePlayer(caster.Script);

			return true;
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