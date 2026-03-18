using System;

namespace US13.Core.Editor.ScriptableObjectBrowser
{
	public static class FuzzyMatcher
	{
		/// <summary>
		/// Returns a score > 0 if query is a fuzzy subsequence match of text, or 0 if no match.
		/// Higher score = better match.
		/// Scoring: +10 per matched char, +5 bonus for consecutive matches, +8 bonus for matching
		/// at a word boundary (after '.', uppercase following lowercase = camelCase split).
		/// Shorter texts get a small bonus to prefer concise names.
		/// </summary>
		public static int Score(string text, string query)
		{
			if (string.IsNullOrEmpty(query)) return 1; // empty query matches everything
			if (string.IsNullOrEmpty(text)) return 0;

			int textLen = text.Length;
			int queryLen = query.Length;
			int ti = 0;
			int qi = 0;
			int score = 0;
			bool previousMatched = false;

			while (ti < textLen && qi < queryLen)
			{
				char tc = char.ToLowerInvariant(text[ti]);
				char qc = char.ToLowerInvariant(query[qi]);

				if (tc == qc)
				{
					score += 10;

					// Consecutive match bonus
					if (previousMatched)
					{
						score += 5;
					}

					// Word boundary bonus: start of string, after '.', or camelCase boundary
					if (ti == 0
						|| text[ti - 1] == '.'
						|| text[ti - 1] == '_'
						|| (char.IsUpper(text[ti]) && ti > 0 && char.IsLower(text[ti - 1])))
					{
						score += 8;
					}

					previousMatched = true;
					qi++;
				}
				else
				{
					previousMatched = false;
				}

				ti++;
			}

			// All query chars must be matched
			if (qi < queryLen) return 0;

			// Bonus for shorter names (prefer concise matches)
			score += Math.Max(0, 100 - textLen);

			return score;
		}
	}
}
