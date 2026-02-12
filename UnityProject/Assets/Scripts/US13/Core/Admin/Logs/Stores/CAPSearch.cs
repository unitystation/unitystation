using System;
using Logs;

namespace US13.Core.Admin.Logs.Stores
{
	public class SearchStep
	{
		public CompareOperation? SearchOperation;

		public bool IsNot;
		public string LookFor;

		public SearchStep SearchStep1;
		public SearchStep SearchStep2;

		public bool HasBracket = false;

		public bool Matches(string Input)
		{
			switch (SearchOperation)
			{
				case CompareOperation.HAS:
					if (IsNot)
					{
						return (Input.Contains(LookFor) == false);
					}
					else
					{
						return Input.Contains(LookFor);
					}
				case CompareOperation.OR:
					return (SearchStep1.Matches(Input) || SearchStep2.Matches(Input)) == (!IsNot);
				case CompareOperation.AND:
					return (SearchStep1.Matches(Input) && SearchStep2.Matches(Input)) == (!IsNot);
				default:
					return false;
			}
		}

		//((cat OR bob) AND NOT Mike) or fat
		//so Is going to be
		//cat Mike and the car drove over the hill
		//so Is OR
		//and AND
		//
		//Make NOT A pre-qualifier so AND NOT , OR NOT
		//
		//((cat OR bob) AND NOT Mike) or fat
		//so With the recursive then you'd be passed
		//{No point optimising which one it Using is since it  Supposed to be simple}
		//OR SearchStep1 -> (HAS fat) , SearchStep2 ->
		//AND -> SearchStep1 -> (HAS NOT Mike) , SearchStep2 ->
		//OR -> SearchStep1 -> (HAS cat), SearchStep2 -> (HAS bob)

		//so now Traversing this
		//humm Start wiith the first subset
		//bob2 AND {{cat OR bob} AND NOT Mike} or fat
		// it steps along until it finds a first control character of " AND ", " OR ",
		//If it's "{", "}" then Does some custom logic
		//Pulls out string from start of and then to control character, and sets that as LookFor
		//Of course it's the SearchOperation,
		//so That would be  (SearchStep1 has)bob2 , SearchOperation AND
		//Now It makes a new SearchStep2, With the starting point of After the " AND ", So that would be { Moves start along If it finds another one {, Makes a new
		//now it's in the new Finds cat with OR bob
		//Goes up earlier AND NOT Mike,
		//so How on earth does it handle the OR, since it's found the left and right
		//maybe If it's at the root Nothing above, Then continues on, And if it finds a OR, AND and then it squashes the route into a new thing and then starts again on the other side


		//Problem what about someone doing bob and cat or meme
		//Left to right,  You assume brackets around everything -> {bob} AND {cat or meme}
		//so, {bob}  = has  Search Step1  AND , Search Step2 ->
	}

	public enum CompareOperation
	{
		HAS,
		OR,
		AND,
	}

	public static class SearchCAP
	{
		public static SearchStep ParseSearch(string input)
		{
			input = input.Replace("ObjName=", @"""ObjectName"":""");
			input = input.Replace("Obj=", @"""Object"":");
			input = input.Replace("StoredInName=", @"""StoredInName"":""");
			input = input.Replace("StoredIn=", @"""StoredIn"":");
			input = input.Replace("PlayerAccount=", @"""PlayerAccountID"":""");
			input = input.Replace("Position=", @"""PositionWorld"":""");
			input = input.Replace("Info=", @"""Info"":""");
			input = input.Trim();
			var Span = input.AsSpan();
			var data = ParseExpression(ref Span);
			//Debug.LogError( JsonConvert.SerializeObject(data));

			return data;
		}

		private static SearchStep ParseExpression(ref ReadOnlySpan<char> input, SearchStep SearchStep = null)
		{
			if (SearchStep == null)
			{
				SearchStep = new SearchStep()
				{
				};
			}

			while (input.IsEmpty == false)
			{
				if (SearchStep.SearchStep1 != null && SearchStep.SearchStep2 != null)
				{
					if (input.IsEmpty == false)
					{
						while (input.IsEmpty == false && input.StartsWith("}", StringComparison.Ordinal))
						{
							input = input.Slice(1);
						}

						var oldroot = SearchStep;
						SearchStep = new SearchStep()
						{
							SearchStep1 = oldroot
						};
						string debug = input.ToString();
						SearchStep = ParseExpression(ref input, SearchStep);
						break;
					}
					else
					{
						//is done
						break;
					}
				}

				string debug2 = input.ToString();
				if (input.StartsWith("{", StringComparison.Ordinal))
				{
					if (SearchStep.HasBracket)
					{
						bool newRootbool = false;
						bool SearchStep1 = false;
						bool SearchStep2 = false;
						SearchStep Tofill = new SearchStep()
						{
						};
						if (SearchStep.SearchStep1 == null)
						{
							SearchStep.SearchStep1 = Tofill;
							SearchStep1 = true;
						}
						else if (SearchStep.SearchStep2 == null)
						{
							SearchStep.SearchStep2 = Tofill;
							SearchStep2 = true;
						}
						else
						{
							newRootbool = true;
							Tofill = new SearchStep()
							{
								SearchStep1 = SearchStep
							};
						}

						//todo HasBracket logical multiple brackets and then something at the end outside of the bracket??

						string debug = input.ToString();
						if (newRootbool)
						{
							SearchStep = ParseExpression(ref input, Tofill);
						}
						else if (SearchStep2)
						{
							SearchStep.SearchStep2 = ParseExpression(ref input, Tofill);
						}
						else if (SearchStep1)
						{
							SearchStep.SearchStep1 = ParseExpression(ref input, Tofill);
						}

						if (newRootbool)
						{
							break;
						}
					}
					else
					{
						if (SearchStep.SearchStep1 != null)
						{
							SearchStep.SearchStep2 = new SearchStep()
							{
							};

							string debug = input.ToString();

							SearchStep.SearchStep2 = ParseExpression(ref input, SearchStep.SearchStep2);
						}
					}


					if (input.StartsWith("{", StringComparison.Ordinal))
					{
						input = input.Slice(1);
					}

					SearchStep.HasBracket = true;
				}

				string devug3 = input.ToString();

				if (input.StartsWith(" AND ", StringComparison.Ordinal) &&
				    (SearchStep.SearchStep1 == null || SearchStep.SearchStep2 == null))
				{
					if ((SearchStep.SearchStep1 == null || SearchStep.SearchStep2 == null) == false)
					{
						Loggy.Error("SHIT!Q");
					}

					SearchStep.SearchOperation = CompareOperation.AND;
					input = input.Slice(5);
				}
				else if (input.StartsWith(" OR ", StringComparison.Ordinal) &&
				         (SearchStep.SearchStep1 == null || SearchStep.SearchStep2 == null))
				{
					if ((SearchStep.SearchStep1 == null || SearchStep.SearchStep2 == null) == false)
					{
						Loggy.Error("SHIT!Q");
					}

					SearchStep.SearchOperation = CompareOperation.OR;
					input = input.Slice(4);
				}
				else if (input.StartsWith("NOT ", StringComparison.Ordinal) &&
				         (SearchStep.SearchStep1 == null || SearchStep.SearchStep2 == null))
				{
					input = input.Slice(4);
					if (SearchStep.SearchStep1 == null)
					{
						SearchStep.SearchStep1 = GetNextToken(ref input, SearchStep, true);
					}
					else
					{
						SearchStep.SearchStep2 = GetNextToken(ref input, SearchStep, true);
					}
				}
				else if (input.StartsWith("{", StringComparison.Ordinal) == false &&
				         input.StartsWith("}", StringComparison.Ordinal) == false &&
				         (SearchStep.SearchStep1 == null || SearchStep.SearchStep2 == null))
				{
					if (SearchStep.SearchStep1 == null)
					{
						SearchStep.SearchStep1 = GetNextToken(ref input, SearchStep, false);
					}
					else
					{
						SearchStep.SearchStep2 = GetNextToken(ref input, SearchStep, false);
					}
				}

				if (input.StartsWith("}", StringComparison.Ordinal))
				{
					input = input.Slice(1);
					if ((SearchStep.SearchStep1 != null && SearchStep.SearchStep2 != null))
					{
						return SearchStep;
					}
					else
					{
						if (SearchStep.HasBracket)
						{
							if (SearchStep.SearchStep1 == null || SearchStep.SearchStep2 == null)
							{
								Loggy.Error("OG NOOOOOOOO!!!!!!!!");
							}

							return SearchStep;
						}
						else
						{
							Loggy.Error("OH NO Invalid boooo");
						}
					}
				}
			}

			if (SearchStep.SearchStep1 == null || SearchStep.SearchStep2 == null)
			{
				if (SearchStep.SearchOperation == null)
				{
					if (SearchStep.SearchStep1 != null)
					{
						SearchStep = SearchStep.SearchStep1;
					}
					else
					{
						SearchStep = SearchStep.SearchStep2;
					}
				}
				else
				{
					Loggy.Error("OG NOOOOOOOO!!!!!!!!");
				}
			}


			return SearchStep;
		}

		private static SearchStep GetNextToken(
			ref ReadOnlySpan<char> input,
			SearchStep OriginatingSearchStep,
			bool IsNot)
		{
			bool loop = true;

			ReadOnlySpan<char> originalInput = input;

			while (loop && !input.IsEmpty)
			{
				if (input.StartsWith("{", StringComparison.Ordinal))
				{
					loop = false;
				}
				else if (input.StartsWith("}", StringComparison.Ordinal))
				{
					loop = false;
				}
				else if (input.StartsWith(" AND ", StringComparison.Ordinal))
				{
					loop = false;
				}
				else if (input.StartsWith(" OR ", StringComparison.Ordinal))
				{
					loop = false;
				}
				else if (input.StartsWith(" NOT ", StringComparison.Ordinal))
				{
					loop = false;
				}
				else
				{
					input = input.Slice(1);
				}
			}

			int consumedLength = originalInput.Length - input.Length;
			return new SearchStep()
			{
				SearchOperation = CompareOperation.HAS,
				LookFor = originalInput.Slice(0, consumedLength).ToString(),
				IsNot = IsNot
			};
		}
	}
}