using System;
using Logs;

namespace Util.Independent.FluentRichText.Styles
{
	public class AlphaStrategy : IStyleStrategy
	{
		private readonly int percentage;

		public AlphaStrategy(int percentage)
		{
			if (percentage is < 0 or > 100)
			{
				Loggy.LogError("RichText received invalid percentage for alpha. Percentage must be between 0 and 100.");
				this.percentage = -1;
				return;
			}

			this.percentage = percentage;
		}

		public string ApplyStyle(string text)
		{
			if (percentage == -1)
			{
				return text;
			}

			int alphaValue = (int)Math.Round(255 * (percentage / 100.0), MidpointRounding.AwayFromZero);
			return $"<alpha=#{alphaValue:X2}>{text}</alpha>";
		}
	}
}