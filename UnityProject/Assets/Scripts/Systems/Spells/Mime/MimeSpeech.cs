namespace Spells
{
	public class MimeSpeech : Spell
	{
		protected override string FormatStillRechargingMessage(ConnectedPlayer caster)
		{
			return caster.Script.mind.IsMiming
				? "You can't break your vow of silence that fast!"
				: "You'll have to wait before you can give your vow of silence again!";
		}

		protected override string FormatInvocationMessageSelf(ConnectedPlayer caster)
		{
			return caster.Script.mind.IsMiming ? "You make a vow of silence." : "You break your vow of silence.";
		}

		public override bool CastSpellServer(ConnectedPlayer caster)
		{
			if (!base.CastSpellServer(caster))
			{
				return false;
			}

			caster.Script.mind.IsMiming = !caster.Script.mind.IsMiming;
			return true;
		}
	}
}