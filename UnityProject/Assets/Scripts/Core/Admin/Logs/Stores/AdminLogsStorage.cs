using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.Admin.Logs.Interfaces;
using Core.Editor.Attributes;
using Initialisation;
using Logs;
using NUnit.Framework;
using SecureStuff;
using Shared.Managers;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace Core.Admin.Logs.Stores
{
	public class AdminLogsStorage : SingletonManager<AdminLogsStorage>, IAdminStorage
	{
		private Queue<LongTermLogEntry> entries = new Queue<LongTermLogEntry>();
		private bool readyForQueue = true;

		public const int ENTRY_PAGE_SIZE = 45;

		[SerializeField, SerializeReference, SelectImplementation(typeof(IAdminLogEntryConverter<string>))]
		private IAdminLogEntryConverter<string> EntryConverter;

		public IAdminLogEntryConverter<string> Converter => EntryConverter;

		public override void Start()
		{
			base.Start();
			AdminLogsManager.OnNewLog += QueueLog;
		}

		public void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		public void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			if (entries.Count == 0) return;
			if (readyForQueue == false) return;
			Store(entries.Dequeue());
		}

		private void QueueLog(LogEntry newEntry)
		{
			if (GameManager.Instance.CurrentRoundState == RoundState.PreRound) return;
			entries.Enqueue(new LongTermLogEntry(newEntry));
		}

		public async Task Store(object entry)
		{
			readyForQueue = false;
			string newLog = "\n";
			try
			{
				newLog = EntryConverter.Convert(entry);
				if (newLog == null)
				{
					Loggy.Error(
						"[AdminLogsStorage/Store()] - Recevied a null entry when attempting to convert logs into a human readable version.");
					readyForQueue = true;
					return;
				}
			}
			catch (Exception e)
			{
				Loggy.Error(e.ToString());
				readyForQueue = true;
				return;
			}

			//TODO: Update this to have operations be IAdminLogEntryConverter specific to allow for things like easy SQLite integretions
			string filePath = Path.Combine("Admin", $"{DateTime.Now:yyyy-MM-dd} - {GameManager.RoundID}.txt");
			CheckForDirectory(filePath);
			await Task.Run(() => { WriteToLogsFile(filePath, newLog); });
			readyForQueue = true;
		}

		private void CheckForDirectory(string filePath)
		{
			if (AccessFile.Exists(filePath, true, FolderType.Logs, false) == false)
			{
				AccessFile.Save(filePath, "", FolderType.Logs, false);
			}
		}

		private void WriteToLogsFile(string filePath, string newLog)
		{
			try
			{
				AccessFile.AppendAllText(filePath, newLog, FolderType.Logs, false);
			}
			catch (UnauthorizedAccessException uae)
			{
				Loggy.Info("Access to the path is denied: " + uae);
			}
			catch (PathTooLongException ptle) //windows reeeeeEEEEEEEEEEE
			{
				Loggy.Info("The specified path, file name, or both are too long: " + ptle);
			}
			catch (IOException ioe)
			{
				Loggy.Info("An I/O error occurred while opening the file: " + ioe);
			}
			catch (Exception ex)
			{
				Loggy.Info("An unexpected error occurred: " + ex);
			}
		}

		public static void AddToEntryList(ref List<LongTermLogEntry> entries, string logLine)
		{
			try
			{
				LongTermLogEntry logEntry = Instance.Converter.ConvertBackSingle(logLine);
				entries.Add(logEntry);
			}
			catch (Exception e)
			{
				Loggy.Error(
					$"[AdminLogsStorage/FetchLogsPaginated()] - Failed to convert log line to LogEntry: {logLine} + " +
					e.ToString());
			}
		}

		public static async Task<List<LongTermLogEntry>> FetchAllLogs(string fileName)
		{
			List<LongTermLogEntry> logEntries = new List<LongTermLogEntry>();
			string filePath = Path.Combine("Admin", fileName);
			try
			{
				if (AccessFile.Exists(filePath, true, FolderType.Logs, false) == false)
				{
					Loggy.Error($"[AdminLogsStorage/FetchLogs()] - File not found: {filePath}");
				}

				string fileContent = await Task.Run(() => AccessFile.Load(filePath, FolderType.Logs, false));
				string[] logLines = fileContent.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
				foreach (string logLine in logLines)
				{
					try
					{
						AddToEntryList(ref logEntries, logLine);
					}
					catch (Exception e)
					{
						Loggy.Error($"[AdminLogsStorage/FetchLogs()] - Exception during log entry conversion: {e}");
					}
				}
			}
			catch (Exception e)
			{
				Loggy.Error($"[AdminLogsStorage/FetchLogs()] - Exception during file read: {e}");
			}

			return logEntries;
		}

		public static async Task<List<LongTermLogEntry>> FetchLogsPaginated(string fileName, int pageNumber,string SearchString,
			int pageSize = ENTRY_PAGE_SIZE)
		{
			async Task<string> LoadData(string filePath)
			{
				var data = "";
				try
				{
					data = await Task.Run(() => AccessFile.Load(filePath, FolderType.Logs, false));
				}
				catch (Exception e)
				{
					Loggy.Error($"[AdminLogsStorage/FetchLogsPaginated()] - Exception during file read: {e}");
				}

				return data;
			}

			if (pageNumber <= 0) pageNumber = 1;
			List<LongTermLogEntry> logEntries = new List<LongTermLogEntry>();
			string filePath = Path.Combine("Admin", fileName);
			try
			{
				if (AccessFile.Exists(filePath, true, FolderType.Logs, false) == false)
				{
					Loggy.Error($"[AdminLogsStorage/FetchLogsPaginated()] - File not found: {filePath}");
				}

				string fileContent = await LoadData(filePath);
				LoadManager.DoInMainThread(() => Loggy.Info("Moving back to main thread."));
				if (string.IsNullOrEmpty(fileContent)) return logEntries;
				IEnumerable<string> logLines = fileContent.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);

				if (string.IsNullOrEmpty(SearchString) == false)
				{
					var SearchObject = ParseSearch(SearchString);
					logLines = logLines.Where(x => SearchObject.Matches(x));
				}

				int skip = (pageNumber - 1) * pageSize;
				int take = pageSize;
				var paginatedLogLines = logLines.Skip(skip).Take(take);
				foreach (string logLine in paginatedLogLines)
				{
					try
					{
						AddToEntryList(ref logEntries, logLine);
					}
					catch (Exception e)
					{
						Loggy.Error(
							$"[AdminLogsStorage/FetchLogsPaginated()] - Exception during log entry conversion: {e}");
					}
				}
			}
			catch (Exception e)
			{
				Loggy.Error($"[AdminLogsStorage/FetchLogsPaginated()] - Exception during file read: {e}");
			}

			return logEntries;
		}

		public class SearchStep
		{
			[JsonConverter(typeof(StringEnumConverter))]
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


				if  (SearchStep.SearchStep1 != null && SearchStep.SearchStep2 != null)
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
							SearchStep.SearchStep2  = Tofill;
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
							SearchStep.SearchStep2  = new SearchStep()
							{
							};

							string debug = input.ToString();

							SearchStep.SearchStep2 = ParseExpression(ref input, SearchStep.SearchStep2 );
						}
					}



					if (input.StartsWith("{", StringComparison.Ordinal))
					{
						input = input.Slice(1);
					}

					SearchStep.HasBracket = true;
				}

				string devug3 = input.ToString();

				if (input.StartsWith(" AND ", StringComparison.Ordinal) && (SearchStep.SearchStep1 == null || SearchStep.SearchStep2 == null ))
				{
					if ((SearchStep.SearchStep1 == null || SearchStep.SearchStep2 == null ) == false)
					{
						Loggy.Error("SHIT!Q");
					}
					SearchStep.SearchOperation = CompareOperation.AND;
					input = input.Slice(5);
				}
				else if (input.StartsWith(" OR ", StringComparison.Ordinal) && (SearchStep.SearchStep1 == null || SearchStep.SearchStep2 == null ))
				{
					if ((SearchStep.SearchStep1 == null || SearchStep.SearchStep2 == null ) == false)
					{
						Loggy.Error("SHIT!Q");
					}
					SearchStep.SearchOperation = CompareOperation.OR;
					input = input.Slice(4);
				}
				else if (input.StartsWith("NOT ", StringComparison.Ordinal) && (SearchStep.SearchStep1 == null || SearchStep.SearchStep2 == null ))
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
				else if (input.StartsWith("{", StringComparison.Ordinal) == false && input.StartsWith("}", StringComparison.Ordinal) == false && (SearchStep.SearchStep1 == null || SearchStep.SearchStep2 == null ))
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
				IsNot =  IsNot
			};
		}


		public static async Task<int> GetTotalPages(string fileName, int pageSize = ENTRY_PAGE_SIZE)
		{
			string filePath = Path.Combine("Admin", fileName);
			int totalEntries = 0;
			try
			{
				if (AccessFile.Exists(filePath, true, FolderType.Logs, false) == false)
				{
					Loggy.Error($"[AdminLogsStorage/GetTotalPages()] - File not found: {filePath}");
					return totalEntries;
				}

				string fileContent = await Task.Run(() => AccessFile.Load(filePath, FolderType.Logs, false));
				LoadManager.DoInMainThread(() => Loggy.Info("Moving back to main thread."));
				string[] logLines = fileContent.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
				totalEntries = logLines.Length;
			}
			catch (Exception e)
			{
				Loggy.Error($"[AdminLogsStorage/GetTotalPages()] - Exception during file read: {e}");
				return 0;
			}

			return (int) Math.Ceiling((double) totalEntries / pageSize);
		}

		public static List<string> GetAllLogFiles()
		{
			List<string> totalEntries = new List<string>();
			if (AccessFile.Exists("Admin", false, FolderType.Logs, false) == false)
			{
				Loggy.Error($"[AdminLogsStorage/GetTotalPages()] - Logs folder not found.");
				return totalEntries;
			}

			string[] files = AccessFile.DirectoriesOrFilesIn("Admin", FolderType.Logs, false);
			var Reversed = files.Reverse();

			int i = 0;

			foreach (string file in Reversed)
			{
				if (i > 100)
				{
					AccessFile.Delete("Admin" + "/"  +file, FolderType.Logs, false);
				}
				else
				{
					totalEntries.Add(file);
				}

				i++;
			}


			return totalEntries;
		}
	}
}