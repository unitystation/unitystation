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
	internal static class HubValidation
	{
		internal static bool? trustedMode = null;

		private static NamedPipeClientStream clientPipe;
		private static StreamReader reader;
		private static StreamWriter writer;

		private static List<string> allowedAPIHosts;
		private static List<string> AllowedAPIHosts
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


		private static List<string> allowedOpenHosts;
		private static List<string> AllowedOpenHosts
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
			public List<string> AllowedOpenHosts = new List<string>();
			public List<string> AllowedAPIHosts = new List<string>();
		}

		private static void LoadCashedURLConfiguration()
		{
			var path = Path.Combine(Application.persistentDataPath, AccessFile.ForkName, "TrustedURLs.json");

			// Check if the file already exists
			if (File.Exists(path) == false)
			{
				// Create the file at the specified path
				File.Create(path).Close();
				File.WriteAllText(path, JsonConvert.SerializeObject(new URLData()));
			}

			var data = JsonConvert.DeserializeObject<URLData>(File.ReadAllText(path));
			allowedOpenHosts = data.AllowedOpenHosts;
			allowedAPIHosts = data.AllowedAPIHosts;
		}

		private static void SaveCashedURLConfiguration(URLData URLData)
		{
			var path = Path.Combine(Application.persistentDataPath, AccessFile.ForkName, "TrustedURLs.json");

			File.WriteAllText(path, JsonConvert.SerializeObject(URLData));

		}

		private static void AddTrustedHost(string Host, bool IsAPI)
		{
			if (IsAPI)
			{
				AllowedAPIHosts.Add(Host);
			}
			else
			{
				AllowedOpenHosts.Add(Host);
			}

			SaveCashedURLConfiguration(new URLData()
			{
				AllowedAPIHosts = allowedAPIHosts,
				AllowedOpenHosts = AllowedOpenHosts
			});
		}

		internal static bool TrustedMode
		{
			get
			{
				if (trustedMode == null)
				{
					string[] commandLineArgs = Environment.GetCommandLineArgs();
					trustedMode = commandLineArgs.Any(x => x == "--trusted");
				}

				return trustedMode.Value;
			}
		}

		public static bool RequestAPIURL(Uri URL, string JustificationReason, bool addAsTrusted)
		{

			if (AllowedAPIHosts.Contains(URL.Host))
			{
				return true;
			}

			writer.WriteLine($"{ClientRequest.API_URL},{URL},{JustificationReason}");
			writer.Flush();

			var APIURL = bool.Parse(reader.ReadLine());
			if (APIURL && addAsTrusted)
			{
				AddTrustedHost(URL.Host, true);
			}


			return APIURL;
		}

		public static bool RequestOpenURL(Uri URL, string justificationReason, bool addAsTrusted)
		{
			if (AllowedOpenHosts.Contains(URL.Host))
			{
				return true;
			}

			writer.WriteLine($"{ClientRequest.URL},{URL},{justificationReason}");
			writer.Flush();

			var openURL = bool.Parse(reader.ReadLine());
			if (openURL && addAsTrusted)
			{
				AddTrustedHost(URL.Host, false);
			}

			return openURL;
		}


		public static bool RequestTrustedMode(string JustificationReason)
		{
			writer.WriteLine($"{ClientRequest.Host_Trust_Mode},{JustificationReason}");
			writer.Flush();

			var IsTrusted = bool.Parse(reader.ReadLine());
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

		static  HubValidation()
		{
			clientPipe = new NamedPipeClientStream(".", "Unitystation Hub<-->Build Communication", PipeDirection.InOut);
			clientPipe.Connect();
			reader = new StreamReader(clientPipe);
			writer = new StreamWriter(clientPipe);
		}

	}
}