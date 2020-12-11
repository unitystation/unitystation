using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Anything that needs to run straight after a build should go in this script
/// BuildReport can be used to determine which platform the build target is set to 
/// by checking report.summary.platform enum
/// </summary>
class PostBuildActions : IPostprocessBuildWithReport
{
	public int callbackOrder { get { return 0; } }

	public void OnPostprocessBuild(BuildReport report)
	{
		//Reload the scene after building to refresh it
		AssetDatabase.SaveAssets();
	}
}
