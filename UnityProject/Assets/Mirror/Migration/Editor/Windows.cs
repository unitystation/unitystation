using UnityEditor;
using Mirror.MigrationUtilities;

public class Windows : EditorWindow {

    [MenuItem("Tools/Mirror/Migrate UNET components on Prefabs")]
    private static void ReplaceComponentsOnPrefabs() {
        if (EditorUtility.DisplayDialog("Prefabs Converter",
            "Are you sure you want to convert prefabs of your project from UNET to Mirror?" + 
            "\nNote: Depending on your project size, it could take lot of time. Please don't close Unity during the process to avoid corrupted project." +
            "\nAlso, please be sure you made a backup of your project, just in case.",
            "Yes, farewell UNET!", "Cancel")) {

            Components.FindAndReplaceUnetComponents(out int netComponentObsolete);

            if (netComponentObsolete > 0) {
                EditorUtility.DisplayDialog("Warning",
                    "Please check your console logs, obsolete components found.",
                    "OK");
            }
        }
    }

    [MenuItem("Tools/Mirror/Migrate UNET components on Scene")]
    private static void ReplaceComponentsOnScene() {
        if (EditorUtility.DisplayDialog("Scene GameObjects Converter",
            "Are you sure you want to convert GameObjects of your scene from UNET to Mirror?" +
            "\nNote: Depending on your scene size, it could take lot of time. Please don't close Unity during the process to avoid corrupted scene." +
            "\nAlso, please be sure you made a backup of your project, just in case.",
            "Yes, farewell UNET!", "Cancel")) {

            Components.FindAndReplaceUnetSceneGameObject(out int netComponentObsolete);

            if (netComponentObsolete > 0) {
                EditorUtility.DisplayDialog("Warning",
                    "Please check your console logs, obsolete components found.",
                    "OK");
            }
        }
    }

    [MenuItem("Tools/Mirror/Migrate scripts from UNET")]
    public static void ReplaceScripts() {
        if (EditorUtility.DisplayDialog("Scripts Converter", "Welcome to the Migration Tool for Mirror Networking. " +
            "This tool will convert your existing UNET code into the Mirror equivalent code.\n\nBefore we begin, we STRONGLY " +
            "recommend you take a full backup of your project as this tool is not perfect.\n\nWhile it does not attempt to " +
            "purposefully trash your network scripts, it could break your project. Be smart and BACKUP NOW.",
            "Yes, farewell UNET!", "Cancel")) {

            // User accepted the risks - go ahead!
            Scripts.ScriptsMigration();

            // Cleanup after yourself.
            Scripts.Cleanup();

            // Refresh the asset database, because sometimes Unity will be lazy about it.
            AssetDatabase.Refresh();
        }
    }
}
