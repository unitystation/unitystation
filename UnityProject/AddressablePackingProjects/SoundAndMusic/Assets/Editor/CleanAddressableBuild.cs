using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public static class CleanAddressableBuild
{
	private const string GithubContentUrl =
		"https://raw.githubusercontent.com/unitystation/unitystation/develop/AddressableContent";

	[MenuItem("Tools/Addressables/Clean Build and Deploy")]
	public static void BuildCleanAndDeploy()
	{
		var settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings == null)
		{
			Debug.LogError("[CleanAddressableBuild] No AddressableAssetSettings found.");
			return;
		}

		// Clear ServerData before building to prevent stale files from previous builds
		string serverDataPath = Path.Combine(Application.dataPath, "..", "ServerData");
		if (Directory.Exists(serverDataPath))
		{
			Directory.Delete(serverDataPath, true);
			Directory.CreateDirectory(serverDataPath);
		}

		AddressableAssetSettings.CleanPlayerContent(settings.ActivePlayerDataBuilder);
		Debug.Log("[CleanAddressableBuild] Cleaned old build.");

		AddressableAssetSettings.BuildPlayerContent();
		Debug.Log("[CleanAddressableBuild] Addressable build complete.");

		Deploy();
	}

	private static void Deploy()
	{
		// Navigate from Assets/ up to the repo root:
		// Assets/ -> SoundAndMusic/ -> AddressablePackingProjects/ -> UnityProject/ -> repo root
		string packingProjectsPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
		string repoRoot = Path.GetFullPath(Path.Combine(packingProjectsPath, "..", ".."));
		string contentPath = Path.Combine(repoRoot, "AddressableContent");

		if (Directory.Exists(contentPath))
		{
			Directory.Delete(contentPath, true);
			Debug.Log("[CleanAddressableBuild] Cleared AddressableContent folder.");
		}

		Directory.CreateDirectory(contentPath);

		foreach (string bundlePath in Directory.GetDirectories(packingProjectsPath))
		{
			string bundleName = Path.GetFileName(bundlePath);
			string serverDataPath = Path.Combine(bundlePath, "ServerData");
			if (Directory.Exists(serverDataPath) == false) continue;

			string destPath = Path.Combine(contentPath, bundleName);
			Directory.CreateDirectory(destPath);

			foreach (string file in Directory.GetFiles(serverDataPath))
			{
				string destFile = Path.Combine(destPath, Path.GetFileName(file));
				File.Copy(file, destFile);
			}

			string catalogFile = Directory.GetFiles(destPath, "*.json").Select(Path.GetFileName).FirstOrDefault();
			if (catalogFile == null)
			{
				Debug.LogError($"[CleanAddressableBuild] No catalog JSON found for {bundleName}.");
				continue;
			}

			// Create the txt file pointing to the catalog URL
			string txtPath = Path.Combine(destPath, $"{bundleName}.txt");
			File.WriteAllText(txtPath, $"{GithubContentUrl}/{bundleName}/{catalogFile}");

			// Fix internal bundle paths in the catalog JSON to point to GitHub CDN
			CorrectCatalogJson(Path.Combine(destPath, catalogFile), bundleName);

			Debug.Log($"[CleanAddressableBuild] Deployed {bundleName}.");
		}

		Debug.Log("[CleanAddressableBuild] Deploy complete.");
	}

	private static void CorrectCatalogJson(string filePath, string bundleName)
	{
		string json = File.ReadAllText(filePath);

		// Match any JSON string value containing "AddressablePackingProjects" and replace
		// with the GitHub CDN URL pointing to just the filename.
		string corrected = System.Text.RegularExpressions.Regex.Replace(json,
			@"""([^""]*AddressablePackingProjects[^""]*)""",
			match =>
			{
				string entry = match.Groups[1].Value;
				string fileName = Path.GetFileName(entry);
				return $"\"{GithubContentUrl}/{bundleName}/{fileName}\"";
			});

		File.WriteAllText(filePath, corrected);
	}
}
