using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;


public class SetUpENetMac : IPostprocessBuildWithReport
{
	public int callbackOrder { get { return 1; } }
	public void OnPostprocessBuild(BuildReport report)
	{
		if (report.summary.platform == BuildTarget.StandaloneOSX)
		{
			var toCopyTo = report.summary.outputPath + "/Contents/PlugIns/libenet.dylib";
			var toCopyFrom = Application.dataPath + "/Mirror/Runtime/Transports/Ignorance/Plugins/macOS/libenet.dylib";
			var InFile = new FileInfo(toCopyFrom);
			InFile.CopyTo(toCopyTo, true);
		}
	}
}
