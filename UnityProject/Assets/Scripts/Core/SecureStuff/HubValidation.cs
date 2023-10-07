using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace SecureStuff
{
	public static class HubValidation
	{
		internal static bool? trustedMode = null;

		private static NamedPipeClientStream clientPipe;
		private static StreamReader reader;
		private static StreamWriter writer;


		private const string githubusercontent = "raw.githubusercontent.com";

		private static HashSet<string> allowedAPIHosts;


		private static HashSet<string> AllowedAPIHosts
		{
			get
			{
				if (allowedAPIHosts == null)
				{
					LoadCashedURLConfiguration();
				}

				return allowedAPIHosts;
			}
		}


		private static HashSet<string> allowedGithubRepositories;

		private static HashSet<string> AllowedGithubRepositories
		{
			get
			{
				if (allowedGithubRepositories == null)
				{
					LoadCashedURLConfiguration();
				}

				return allowedGithubRepositories;
			}
		}


		private static HashSet<string> allowedOpenHosts;
		private static HashSet<string> AllowedOpenHosts
		{
			get
			{
				if (allowedOpenHosts == null)
				{
					LoadCashedURLConfiguration();
				}

				return allowedOpenHosts;
			}
		}

		private enum ClientRequest
		{
			URL = 1,
			API_URL = 2,
			Host_Trust_Mode = 3,
		}

		private class URLData
		{
			public HashSet<string> SavedAllowedOpenHosts = new HashSet<string>();
			public HashSet<string> SavedAllowedAPIHosts = new HashSet<string>();
			public HashSet<string> SavedAllowedGithubRepositories = new HashSet<string>();

		}

		private static async Task<bool> SetUp(string OnFailText, string URLClipboard = "")
		{
			int timeout = 5000;
			clientPipe = new NamedPipeClientStream(".", "Unitystation_Hub_Build_Communication", PipeDirection.InOut);
			var task = clientPipe.ConnectAsync();
			if (await Task.WhenAny(task, Task.Delay(timeout)) == task) {
				reader = new StreamReader(clientPipe);
				writer = new StreamWriter(clientPipe);
				return true;
			} else {
				HubNotConnectedPopUp.Instance.SetUp(OnFailText, URLClipboard);
				return false;
			}

		}

		private static void LoadCashedURLConfiguration()
		{
			var path = Path.Combine(Application.persistentDataPath, AccessFile.ForkName, "TrustedURLs.json");


			// Check if the file already exists
			if (File.Exists(path) == false)
			{
				// Create the file at the specified path
				File.Create(path).Close();
				File.WriteAllText(path, @"
{
    ""SavedAllowedOpenHosts"": [],
    ""SavedAllowedAPIHosts"": [""api.unitystation.org"", ""firestore.googleapis.com"", ""play.unitystation.org""],
    ""SavedAllowedGithubRepositories"": [""unitystation/unitystation/develop""]
}");
			}

			var data = JsonConvert.DeserializeObject<URLData>(File.ReadAllText(path));
			allowedOpenHosts = data.SavedAllowedOpenHosts;
			allowedAPIHosts = data.SavedAllowedAPIHosts;
			allowedGithubRepositories = data.SavedAllowedGithubRepositories;
		}

		private static void SaveCashedURLConfiguration(URLData URLData)
		{
			var path = Path.Combine(Application.persistentDataPath, AccessFile.ForkName, "TrustedURLs.json");

			Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, AccessFile.ForkName));

			File.WriteAllText(path, JsonConvert.SerializeObject(URLData));

		}

		private static void AddTrustedHost(Uri url, bool isAPI)
        {
            if (isAPI)
            {
                if (url.Host == githubusercontent)
                {
                    var segments = url.Segments;

                    // Expected format: /username/reponame/branchname/...
                    if (segments.Length >= 4)
                    {
                        var username = segments[1].TrimEnd('/');
                        var repoName = segments[2].TrimEnd('/');
                        var branchName = segments[3].TrimEnd('/');

                        AllowedGithubRepositories.Add($"{username.ToLower()}/{repoName.ToLower()}/{branchName.ToLower()}");
                    }
                }
                else
                {
                    AllowedAPIHosts.Add(url.Host);
                }
            }
            else
            {
                AllowedOpenHosts.Add(url.Host);
            }

            SaveCashedURLConfiguration(new URLData()
            {
                SavedAllowedAPIHosts = AllowedAPIHosts,
                SavedAllowedOpenHosts = AllowedOpenHosts,
                SavedAllowedGithubRepositories = AllowedGithubRepositories
            });
        }

		public static bool TrustedMode
		{
			get
			{
				return true;
#if UNITY_EDITOR
				return true;
#endif

				if (trustedMode == null)
				{
					string[] commandLineArgs = Environment.GetCommandLineArgs();
					trustedMode = commandLineArgs.Any(x => x == "--trusted");
				}

				return trustedMode.Value;
			}
		}



		public static bool CheckWhiteList(Uri URL)
		{
			if (URL.Host == githubusercontent)
			{
				var segments = URL.Segments;

				// Expected format: /username/reponame/branchname/...
				if (segments.Length >= 4)
				{
					var username = segments[1].TrimEnd('/');
					var repoName = segments[2].TrimEnd('/');
					var branchName = segments[3].TrimEnd('/');

					return AllowedGithubRepositories.Contains($"{username.ToLower()}/{repoName.ToLower()}/{branchName.ToLower()}");
				}
				else
				{
					return false;
				}
			}

			if (AllowedAPIHosts.Contains(URL.Host))
			{
				return true;
			}
			return false;
		}

		public static async Task<bool> RequestAPIURL(Uri URL, string JustificationReason, bool addAsTrusted)
		{
			if (TrustedMode) return true;
			if (CheckWhiteList(URL))
			{
				return true;
			}

			var AbleToConnect = true;
			if (writer == null || (clientPipe != null && clientPipe.IsConnected == false))
			{
				AbleToConnect = await SetUp($" Wasn't able to connect the hub to Evaluate new domain for API Request URL {URL}, The hub is used as a secure method for getting user input ");
			}

			if (AbleToConnect == false)
			{
				return false;
			}


			writer.WriteLine($"{ClientRequest.API_URL},{URL},{JustificationReason}");
			writer.Flush();

			var APIURL = bool.Parse(await reader.ReadLineAsync());
			if (APIURL && addAsTrusted)
			{
				AddTrustedHost(URL, true);
			}


			return APIURL;
		}

		public static async Task<bool> RequestOpenURL(Uri URL, string justificationReason, bool addAsTrusted)
		{
			if (TrustedMode) return true;
			if (AllowedOpenHosts.Contains(URL.Host))
			{
				return true;
			}

			var AbleToConnect = true;
			if (writer == null || (clientPipe != null && clientPipe.IsConnected == false))
			{
				AbleToConnect = await SetUp(
					$" Wasn't able to connect the hub to Get user input on open URL {URL}, The hub is used as a secure method for getting user input ",
					URL.ToString());
			}

			if (AbleToConnect == false)
			{
				return false;
			}

			writer.WriteLine($"{ClientRequest.URL},{URL},{justificationReason}");
			writer.Flush();

			var openURL = bool.Parse(reader.ReadLine());
			if (openURL && addAsTrusted)
			{
				AddTrustedHost(URL, false);
			}

			return openURL;
		}


		public static async Task<bool> RequestTrustedMode(string JustificationReason)
		{
			if (TrustedMode) return true;
			var AbleToConnect = true;
			if (writer == null || (clientPipe != null && clientPipe.IsConnected == false))
			{
				AbleToConnect = await SetUp($" Wasn't able to connect the hub to Turn on trusted mode " +
				                            $"(Access to Verbal viewer on client side, automatic yes to API and open URL requests)," +
				                            $" The hub is used as a secure method for getting user input ");;
			}

			if (AbleToConnect == false)
			{
				return false;
			}

			await writer.WriteLineAsync($"{ClientRequest.Host_Trust_Mode},{JustificationReason}");
			await writer.FlushAsync();


			bool IsTrusted = bool.Parse(await reader.ReadLineAsync());
			if (IsTrusted)
			{
				trustedMode = true;
			}
			else
			{
				trustedMode = false;
			}

			return IsTrusted;
		}
	}
}