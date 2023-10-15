using System.Reflection;
using System.Text.Json;
using UnitystationLauncher.ContentScanning;

namespace CodeScan;

public static class ThisMain
{

	public static List<string> Errors = new List<string>();

	public static List<string> Infos = new List<string>();

	public static HashSet<string> goodFiles = new HashSet<string>()
	{};




	public static async Task Main(string[] args)
	{



		try
		{
			Action<string> info = new Action<string>((string log) => { Infos.Add(log); });
			Action<string> error = new Action<string>((string log) => { Errors.Add(log);  });

			var goodPath = @"Q:\Fast programmes\ss13 development\unitystation\UnityProject\Build";

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
				ManagedPath = @"Unitystation_Data\Managed";
				OSText = "Windows";
			}
			if (OperatingSystem.IsLinux())
			{
				ManagedPath = @"Unitystation_Data\Managed";
				OSText = "Linux";
			}
			if (OperatingSystem.IsMacOS())
			{
				ManagedPath = @"Mac.app\Contents\Resources\Data\Managed";
				OSText = "Mac";
			}

			DirectoryInfo directory = new DirectoryInfo(Path.Combine(goodPath, ManagedPath));

			var GoodFiles = new DirectoryInfo(
					System.Reflection.Assembly.GetEntryAssembly().Location.Replace("CodeScan.dll", ""))
					.Parent;
			GoodFiles = GoodFiles.CreateSubdirectory("MissingAssemblies");
			GoodFiles = GoodFiles.CreateSubdirectory(OSText);


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
				if (dat.CheckAssembly(file, directory, listy, info,  error) == false)
				{
					Errors.Add($"{file.Name} Failed scanning Cancelling");
				}
			}
		}
		catch (Exception e)
		{
			Errors.Add(e.ToString());
		}


		// Get the location of the executable
		string executablePath = Assembly.GetEntryAssembly().Location;

		// Construct the JSON file path next to the executable
		string jsonFilePath = Path.Combine(Path.GetDirectoryName(executablePath), "errors.json");

		// Serialize the data to JSON and write it to the file
		string json = JsonSerializer.Serialize(Errors, new JsonSerializerOptions { WriteIndented = true });
		File.WriteAllText(jsonFilePath, json);
	}
}