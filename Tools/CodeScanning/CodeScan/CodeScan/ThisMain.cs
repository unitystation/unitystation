using System.CommandLine;
using System.Reflection;
using System.Text.Json;
using Newtonsoft.Json;
using UnitystationLauncher.ContentScanning;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CodeScan;

public static class ThisMain
{
	public static List<string> Errors = new List<string>();

	public static List<string> Infos = new List<string>();

	public static HashSet<string> goodFiles = new HashSet<string>()
		{ };

	public static List<string> FilesToMoveToManaged = new List<string>() { };


	public static async Task Main(string[] args)
	{
		// Get the location of the executable
		string executablePath = Assembly.GetEntryAssembly().Location.Replace("CodeScan.dll", "");

		try
		{
			bool MemoryClearingMode = false;

			foreach (var arg in args)
			{
				if (arg.Contains('@'))
				{
					MemoryClearingMode = true;
				}
			}

			using (StreamReader r = new StreamReader( Path.Combine( executablePath,".."  , "FilesToMoveToManaged.json")))
			{
				string Injson = r.ReadToEnd();
				FilesToMoveToManaged = JsonConvert.DeserializeObject<List<string>>(Injson);
			}


			if (MemoryClearingMode)
			{
			}
			else
			{
				try
				{
					Action<string> info = new Action<string>((string log) => { Infos.Add(log); });
					Action<string> error = new Action<string>((string log) => { Errors.Add(log); });

					var goodPath =
						@"J:/SuperFast Programs/ss13 development/unitystation/Tools/CodeScanning/CodeScan/CodeScan/bin/Debug/net7.0/Windows";

					foreach (var arg in args)
					{
						if (arg.Contains('>'))
						{
							goodPath = arg.Replace(">", "");
							break;
						}
					}


					List<string> multiAssemblyReference = new List<string>();
					var dat = new AssemblyTypeChecker();

					var OSText = "";
					var ManagedPath = "";

					if (OperatingSystem.IsWindows())
					{
						ManagedPath = @"Unitystation_Data/Managed";
						OSText = "Windows";
					}

					if (OperatingSystem.IsLinux())
					{
						ManagedPath = @"Unitystation_Data/Managed";
						OSText = "Linux";
					}

					if (OperatingSystem.IsMacOS())
					{
						ManagedPath = @"Mac.app/Contents/Resources/Data/Managed";
						OSText = "Mac";
					}

					DirectoryInfo directory = new DirectoryInfo(Path.Combine(goodPath, ManagedPath));

					var GoodFiles = new DirectoryInfo(
							System.Reflection.Assembly.GetEntryAssembly().Location.Replace("CodeScan.dll", ""))
						.Parent;
					GoodFiles = GoodFiles.CreateSubdirectory("MissingAssemblies");
					GoodFiles = GoodFiles.CreateSubdirectory(OSText);

					// Get a list of files in the source directory
					var files = directory.GetFiles();

					// Copy files that match the specified patterns
					foreach (var file in files)
					{
						string fileName = file.Name;

						// Check if any pattern in the list matches the file name
						if (FilesToMoveToManaged.Any(Name => fileName.Contains(Name)))
						{
							string destinationFilePath = Path.Combine(GoodFiles.ToString(), fileName);
							File.Copy(file.ToString(), destinationFilePath, true);
							Console.WriteLine($"Copied: {fileName}");
						}
					}


					goodFiles.UnionWith(GoodFiles
						.GetFiles().Select(x => x.Name).ToHashSet());


					foreach (var file in directory.GetFiles())
					{
						if (goodFiles.Contains(file.Name)) continue;
						if (file.Extension == ".pdb") continue;
						multiAssemblyReference.Add(Path.GetFileNameWithoutExtension(file.Name));
					}


					foreach (var file in directory.GetFiles())
					{
						var listy = multiAssemblyReference.ToList();
						if (goodFiles.Contains(file.Name)) continue;
						if (file.Extension == ".pdb") continue;
						listy.Remove(Path.GetFileNameWithoutExtension(file.Name));
						if (dat.CheckAssembly(file, directory, listy, info, error) == false)
						{
							Errors.Add($"{file.Name} Failed scanning Cancelling");
						}
					}
				}
				catch (Exception e)
				{
					Errors.Add(e.ToString());
				}
			}
		}
		catch (Exception e)
		{
			Errors.Add(e.ToString());
		}



		// Construct the JSON file path next to the executable
		string jsonFilePath = Path.Combine(Path.GetDirectoryName(executablePath), "errors.json");

		// Serialize the data to JSON and write it to the file
		string json = JsonSerializer.Serialize(Errors, new JsonSerializerOptions {WriteIndented = true});
		File.WriteAllText(jsonFilePath, json);
	}
}