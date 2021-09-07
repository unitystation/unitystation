namespace Items.Dice
{
	public class RollD20 : RollDie
	{
		protected override string GetMessage()
		{
			string msg = base.GetMessage();

			if (sides == 20)
			{
				if (result == 1) return msg + " Ouch! Bad luck.";
				else if (result == 20) return msg + " NAT 20!";
			}

			return msg;
		}
	}
}
