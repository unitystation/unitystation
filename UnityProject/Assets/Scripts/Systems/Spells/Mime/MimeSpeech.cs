namespace Systems.Spells
{
	public class MimeSpeech : Spell
	{
		protected override string FormatStillRechargingMessage(PlayerInfo caster)
		{
			return caster.Script.Mind.IsMiming
				? "You can't break your vow of silence that fast!"
				: "You'll have to wait before you can give your vow of silence again!";
		}

		protected override string FormatInvocationMessageSelf(PlayerInfo caster)
		{
			return caster.Script.Mind.IsMiming ? "You make a vow of silence." : "You break your vow of silence.";
		}

		public override bool CastSpellServer(PlayerInfo caster)
		{
			if (!base.CastSpellServer(caster))
			{
				return false;
			}

			caster.Script.Mind.IsMiming = !caster.Script.Mind.IsMiming;
			return true;
		}
	}
}
