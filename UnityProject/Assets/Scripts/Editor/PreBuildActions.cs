using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Anything that needs to run before a build process commences should go in this script
/// BuildReport can be used to determine which platform the build target is set to 
/// by checking report.summary.platform enum
/// </summary>
class PreBuildActions : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildReport report)
    {
        SetPlayerSettingsTitle();
    }

    //This sets the product name of the build to the title of the
    //last git commit / merge
    //TODO: Remove this after release of 0.4 beta
    private async void SetPlayerSettingsTitle()
    {
        var process = new Process();
        process.StartInfo.FileName = "git";
        process.StartInfo.Arguments = "log -1 --pretty=%f";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        PlayerSettings.productName = "unitystation - Latest commit: " + output;
        await process.WaitForExitAsync();
    }
}