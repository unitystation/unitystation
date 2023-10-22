using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using CodeScan;
using Newtonsoft.Json;
using Pidgin;

namespace UnitystationLauncher.ContentScanning

{
	public sealed partial class AssemblyTypeChecker
	{
		private static string NameConfig = @"CodeScanList.json";

		private async Task<SandboxConfig> LoadConfig()
		{
			var httpClient = new HttpClient();
			var response =
				await httpClient.GetAsync(
					"https://raw.githubusercontent.com/unitystation/unitystation/develop/CodeScanList.json");
			if (response.IsSuccessStatusCode)
			{
				var jsonData = await response.Content.ReadAsStringAsync();
				try
				{
					var data = JsonConvert.DeserializeObject<SandboxConfig>(jsonData);

					if (data == null)
					{
						//Log.Error("unable to de-serialise config");
						ThisMain.Errors.Add("unable to de-serialise config");
						throw new DataException("unable to de-serialise config");
					}

					foreach (var @namespace in data.Types)
					{
						foreach (var @class in @namespace.Value)
						{
							ParseTypeConfig(@class.Value);
						}
					}

					return data;
				}
				catch (Exception e)
				{
					ThisMain.Errors.Add(e.ToString());
					Console.WriteLine(e);
					throw;
				}
			}
			else
			{
				ThisMain.Errors.Add("Unable to download config" + response.ToString());
			}

			return null;
		}

		private static void ParseTypeConfig(TypeConfig cfg)
		{
			if (cfg.Methods != null)
			{
				var list = new List<WhitelistMethodDefine>();
				foreach (var m in cfg.Methods)
				{
					try
					{
						list.Add(Parsers.MethodParser.ParseOrThrow(m));
					}
					catch (ParseException e)
					{
						ThisMain.Errors.Add($"Parse exception for '{m}': {e}");
						return;
					}
				}

				cfg.MethodsParsed = list.ToArray();
			}
			else
			{
				cfg.MethodsParsed = Array.Empty<WhitelistMethodDefine>();
			}

			if (cfg.Fields != null)
			{
				var list = new List<WhitelistFieldDefine>();
				foreach (var f in cfg.Fields)
				{
					try
					{
						list.Add(Parsers.FieldParser.ParseOrThrow(f));
					}
					catch (ParseException e)
					{
						ThisMain.Errors.Add($"Parse exception for '{f}': {e}");
						return;
					}
				}

				cfg.FieldsParsed = list.ToArray();
			}
			else
			{
				cfg.FieldsParsed = Array.Empty<WhitelistFieldDefine>();
			}

			if (cfg.NestedTypes != null)
			{
				foreach (var nested in cfg.NestedTypes.Values)
				{
					ParseTypeConfig(nested);
				}
			}
		}
	}
}