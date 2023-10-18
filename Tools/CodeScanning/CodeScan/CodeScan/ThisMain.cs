using System.CommandLine;
using System.Reflection;
using System.Text.Json;
using UnitystationLauncher.ContentScanning;

namespace CodeScan;

public static class ThisMain
{
	public static List<string> Errors = new List<string>();

	public static List<string> Infos = new List<string>();

	public static HashSet<string> goodFiles = new HashSet<string>()
		{ };

	public static List<string> FilesToMoveToManaged = new List<string>()
	{
		"C5",
		"FastScriptReload.Tests.Runtime",
		"Firebase.App",
		"Firebase.Auth",
		"Firebase.Firestore",
		"Firebase.Platform",
		"Firebase.Storage",
		"Firebase.TaskExtension",
		"Google.MiniJson",
		"kcp2k",
		"Logger",
		"Mirror.Authenticators",
		"Mirror.Components",
		"Mirror",
		"Mirror.Examples",
		"Mirror.Ignorance",
		"Mirror.Transports",
		"Mono.Security",
		"mscorlib",
		"netstandard",
		"Newtonsoft.Json",
		"nunit.framework",
		"PlayerPrefsEditor",
		"SecureStuff",
		"SimpleWebTransport",
		"SunVoxplugin",
		"System.ComponentModel.Composition",
		"System.Configuration",
		"System.Core",
		"System.Data.DataSetExtensions",
		"System.Data",
		"System",
		"System.Drawing",
		"System.EnterpriseServices",
		"System.IO.Compression",
		"System.IO.Compression.FileSystem",
		"System.Net.Http",
		"System.Numerics",
		"System.Runtime.Serialization",
		"System.Security",
		"System.ServiceModel.Internals",
		"System.Transactions",
		"System.Xml",
		"System.Xml.Linq",
		"Telepathy",
		"Tomlyn",
		"UniTask.Addressables",
		"UniTask",
		"UniTask.DOTween",
		"UniTask.Linq",
		"UniTask.TextMeshPro",
		"Unity.2D.PixelPerfect",
		"Unity.2D.Tilemap.Extras",
		"Unity.Addressables",
		"Unity.Compat",
		"Unity.InternalAPIEngineBridge.003",
		"Unity.MemoryProfiler",
		"Unity.Multiplayer.Playmode.Common.Runtime",
		"Unity.Multiplayer.Playmode",
		"Unity.Postprocessing.Runtime",
		"Unity.ResourceManager",
		"Unity.ScriptableBuildPipeline",
		"Unity.Tasks",
		"Unity.TextMeshPro",
		"Unity.VectorGraphics",
		"UnityEngine.AccessibilityModule",
		"UnityEngine.AIModule",
		"UnityEngine.AndroidJNIModule",
		"UnityEngine.AnimationModule",
		"UnityEngine.ARModule",
		"UnityEngine.AssetBundleModule",
		"UnityEngine.AudioModule",
		"UnityEngine.ClothModule",
		"UnityEngine.ClusterInputModule",
		"UnityEngine.CommandStateObserverModule",
		"UnityEngine.ContentLoadModule",
		"UnityEngine.CoreModule",
		"UnityEngine.CrashReportingModule",
		"UnityEngine.DirectorModule",
		"UnityEngine",
		"UnityEngine.DSPGraphModule",
		"UnityEngine.GameCenterModule",
		"UnityEngine.GIModule",
		"UnityEngine.GraphToolsFoundationModule",
		"UnityEngine.GridModule",
		"UnityEngine.HotReloadModule",
		"UnityEngine.ImageConversionModule",
		"UnityEngine.IMGUIModule",
		"UnityEngine.InputLegacyModule",
		"UnityEngine.InputModule",
		"UnityEngine.JSONSerializeModule",
		"UnityEngine.LocalizationModule",
		"UnityEngine.MarshallingModule",
		"UnityEngine.NVIDIAModule",
		"UnityEngine.ParticleSystemModule",
		"UnityEngine.PerformanceReportingModule",
		"UnityEngine.Physics2DModule",
		"UnityEngine.PhysicsModule",
		"UnityEngine.ProfilerModule",
		"UnityEngine.PropertiesModule",
		"UnityEngine.RuntimeInitializeOnLoadManagerInitializerModule",
		"UnityEngine.ScreenCaptureModule",
		"UnityEngine.SharedInternalsModule",
		"UnityEngine.SpriteMaskModule",
		"UnityEngine.SpriteShapeModule",
		"UnityEngine.StreamingModule",
		"UnityEngine.SubstanceModule",
		"UnityEngine.SubsystemsModule",
		"UnityEngine.TerrainModule",
		"UnityEngine.TerrainPhysicsModule",
		"UnityEngine.TextCoreFontEngineModule",
		"UnityEngine.TextCoreTextEngineModule",
		"UnityEngine.TextRenderingModule",
		"UnityEngine.TilemapModule",
		"UnityEngine.TLSModule",
		"UnityEngine.UI",
		"UnityEngine.UIElementsModule",
		"UnityEngine.UIModule",
		"UnityEngine.UmbraModule",
		"UnityEngine.UnityAnalyticsCommonModule",
		"UnityEngine.UnityAnalyticsModule",
		"UnityEngine.UnityConnectModule",
		"UnityEngine.ClusterRendererModule",
		"UnityEngine.UnityCurlModule",
		"UnityEngine.UnityTestProtocolModule",
		"UnityEngine.UnityWebRequestAssetBundleModule",
		"UnityEngine.UnityWebRequestAudioModule",
		"UnityEngine.UnityWebRequestModule",
		"UnityEngine.UnityWebRequestTextureModule",
		"UnityEngine.UnityWebRequestWWWModule",
		"UnityEngine.VehiclesModule",
		"UnityEngine.VFXModule",
		"UnityEngine.VideoModule",
		"UnityEngine.VirtualTexturingModule",
		"UnityEngine.VRModule",
		"UnityEngine.WindModule",
		"UnityEngine.XRModule",
		"websocket-sharp",
		"YamlDotNet",
		"YamlDotNet.Examples"
	};


	public static async Task Main(string[] args)
	{
		bool MemoryClearingMode = false;

		foreach (var arg in args)
		{
			if (arg.Contains('@'))
			{
				MemoryClearingMode = true;
			}
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

				var goodPath = @"Q:\Fast programmes\ss13 development\unitystation\UnityProject\Build\Windows";

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

			// Get the location of the executable
			string executablePath = Assembly.GetEntryAssembly().Location.Replace("CodeScan.dll", "");

			// Construct the JSON file path next to the executable
			string jsonFilePath = Path.Combine(Path.GetDirectoryName(executablePath), "errors.json");

			// Serialize the data to JSON and write it to the file
			string json = JsonSerializer.Serialize(Errors, new JsonSerializerOptions {WriteIndented = true});
			File.WriteAllText(jsonFilePath, json);
		}
	}
}