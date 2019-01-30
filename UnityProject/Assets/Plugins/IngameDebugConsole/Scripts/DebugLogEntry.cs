using UnityEngine;

namespace IngameDebugConsole
{
	/// <summary>
	/// Container for a simple debug entry
	/// </summary>
	public class DebugLogEntry : System.IEquatable<DebugLogEntry>
	{
		private const int HASH_NOT_CALCULATED = -623218;

		public string logString;
		public string stackTrace;

		private string completeLog = null;

		/// <summary>
		/// Sprite to show with this entry
		/// </summary>
		public Sprite logTypeSpriteRepresentation;

		/// <summary>
		/// Collapsed count
		/// </summary>
		public int count;

		private int hashValue = HASH_NOT_CALCULATED;

		public DebugLogEntry( string logString, string stackTrace, Sprite sprite )
		{
			this.logString = logString;
			this.stackTrace = stackTrace;

			logTypeSpriteRepresentation = sprite;

			count = 1;
		}

		/// <summary>
		/// Check if two entries have the same origin
		/// </summary>
		/// <param name="other">Other DebugLogEntry object to compare values to</param>
		/// <returns></returns>
		public bool Equals( DebugLogEntry other )
		{
			return this.logString == other.logString && this.stackTrace == other.stackTrace;
		}

		/// <summary>
		/// Return a string containing complete information about this debug entry
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			if( completeLog == null )
				completeLog = string.Concat( logString, "\n", stackTrace );

			return completeLog;
		}

		// Credit: https://stackoverflow.com/a/19250516/2373034
		public override int GetHashCode()
		{
			if( hashValue == HASH_NOT_CALCULATED )
			{
				unchecked
				{
					hashValue = 17;
					hashValue = hashValue * 23 + logString == null ? 0 : logString.GetHashCode();
					hashValue = hashValue * 23 + stackTrace == null ? 0 : stackTrace.GetHashCode();
				}
			}

			return hashValue;
		}
	}
}