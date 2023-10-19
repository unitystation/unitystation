using System.Text;
using JetBrains.Annotations;
using Logs;

namespace Util.Independent.FluentRichText.Styles
{
	public class SpriteStrategy: IStyleStrategy
	{
		[CanBeNull] private readonly string atlas;
		private readonly int? index;
		[CanBeNull] private readonly string name;

		/// <summary>
		/// The instance has index value or name value, but not both.
		/// </summary>
		private bool HasValidParameters => index.HasValue ^ string.IsNullOrEmpty(name) == false;

		private bool HasAtlas => string.IsNullOrEmpty(atlas) == false;

		/// <summary>
		/// Sprite from default atlas by index position
		/// </summary>
		/// <param name="index"></param>
		public SpriteStrategy(int index)
		{
			if (index < 0)
			{
				Loggy.LogError("Rich text sprite index must be greater than or equal to 0.");
				return;
			}

			this.index = index;
		}

		/// <summary>
		/// Sprite from default atlas by name
		/// </summary>
		/// <param name="name"></param>
		public SpriteStrategy(string name)
		{
			this.name = name;
		}

		/// <summary>
		/// Sprite from custom atlas by index
		/// </summary>
		/// <param name="atlas"></param>
		/// <param name="index"></param>
		public SpriteStrategy(string atlas, int index)
		{
			if (index < 0)
			{
				Loggy.LogError("Rich text sprite index must be greater than or equal to 0.");
				return;
			}

			this.atlas = atlas;
			this.index = index;
		}

		/// <summary>
		/// Sprite from custom atlas by name
		/// </summary>
		/// <param name="atlas"></param>
		/// <param name="name"></param>
		public SpriteStrategy(string atlas, string name)
		{
			this.atlas = atlas;
			this.name = name;
		}

		public string ApplyStyle(string text)
		{
			if (HasValidParameters == false)
			{
				Loggy.LogError("Rich text sprite must have either an index or a name, not both.");
				return text;
			}

			StringBuilder sb = new($"{text}<sprite");

			// Append atlas if present
			if (HasAtlas)
			{
				sb.Append($"=\"{atlas}\"");
			}

			// Decide between index and name
			if (index.HasValue)
			{
				sb.Append(HasAtlas ? $" index={index}" : $"={index}");
			}
			else if (string.IsNullOrEmpty(name) == false)
			{
				sb.Append($" name=\"{name}\"");
			}

			sb.Append(">");
			return sb.ToString();
		}
	}
}