using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using EditorConfig.Core;
using UnityEditor;
using UnityEngine;

namespace Mirror.MigrationUtilities {
    public class Scripts : MonoBehaviour {

        // private variables that don't need to be modified.
        const string ScriptExtension = "*.cs";

        public static readonly string[] knownIncompatibleRegexes = {
                "SyncListStruct",
                @"\[Command([^\],]*)\]",
                @"\[ClientRpc([^\],]*)\]",
                @"\[TargetRpc([^\],]*)\]",
                @"\[SyncEvent([^\],]*)\]",
                "NetworkHash128",
                "NetworkInstanceId",
                "GetNetworkSendInterval()",
                "NetworkServer.connections",
                "NetworkServer.connections.Values.Values",
                "NetworkManager.singleton.client",
                "UnityEngine.Networking.NetworkAnimator",
                "UnityEngine.Networking.NetworkBehaviour",
                "UnityEngine.Networking.NetworkClient",
                "UnityEngine.Networking.NetworkConnection",
                "UnityEngine.Networking.NetworkIdentity",
                "UnityEngine.Networking.NetworkManager",
                "UnityEngine.Networking.NetworkProximityChecker",
                "UnityEngine.Networking.NetworkServer",
                "UnityEngine.Networking.NetworkStartPosition",
                "UnityEngine.Networking.NetworkTransform",
                "UnityEngine.Networking.NetworkTransformChild",
                "UnityEngine.Networking.NetworkLobbyManager",
                "UnityEngine.Networking.NetworkLobbyPlayer",
                "UnityEngine.Networking.NetworkManagerHUD",
                "NetworkClient.GetRTT()"
            };

        public static readonly string[] knownCompatibleReplacements = {
                "SyncList",
                "[Command]",
                "[ClientRpc]",
                "[TargetRpc]",
                "[SyncEvent]",
                "System.Guid",
                "uint",
                "syncInterval",
                "NetworkServer.connections.Values",
                "NetworkServer.connections.Values",
                "NetworkClient",
                "Mirror.NetworkAnimator",
                "Mirror.NetworkBehaviour",
                "Mirror.NetworkClient",
                "Mirror.NetworkConnection",
                "Mirror.NetworkIdentity",
                "Mirror.NetworkManager",
                "Mirror.NetworkProximityChecker",
                "Mirror.NetworkServer",
                "Mirror.NetworkStartPosition",
                "Mirror.NetworkTransform",
                "Mirror.NetworkTransformChild",
                "Mirror.NetworkLobbyManager",
                "Mirror.NetworkLobbyPlayer",
                "Mirror.NetworkManagerHUD",
                "((int)(NetworkTime.rtt*1000d))"
            };

        public static readonly string[] notUnetTypes = {
                "CertificateHandler",
                "ChannelQOS",
                "ConnectionConfig",
                "ConnectionSimulatorConfig",
                "DownloadHandler",
                "DownloadHandlerAssetBundle",
                "DownloadHandlerAudioClip",
                "DownloadHandlerBuffer",
                "DownloadHandlerFile",
                "DownloadHandlerMovieTexture",
                "DownloadHandlerScript",
                "DownloadHandlerTexture",
                "GlobalConfig",
                "HostTopology",
                "MultipartFormDataSection",
                "MultipartFormFileSection",
                "NetworkTransport",
                "UnityWebRequest",
                "UnityWebRequestAssetBundle",
                "UnityWebRequestAsyncOperation",
                "UnityWebRequestMultimedia",
                "UnityWebRequestTexture",
                "UploadHandler",
                "UploadHandlerFile",
                "UploadHandlerRaw",
                "Utility"
            };

        static int filesModified = 0;
        static string scriptBuffer = string.Empty;
        static MatchCollection matches;

        // Logic portion begins below.

        public static void ScriptsMigration() {
            // Safeguard in case a developer goofs up
            if (knownIncompatibleRegexes.Length != knownCompatibleReplacements.Length) {
                Debug.LogError("[Mirror Migration Tool] BUG DETECTED: Regexes to search for DO NOT match the Regex Replacements. Cannot continue.\nPlease re-download the converter.");
                return;
            }

            // Place holder for the assets folder location.
            string assetsFolder = Application.dataPath;
            // List structure for the CSharp files.
            List<string> filesToScanAndModify = new List<string>();

            // Be verbose and say what's happening.
            Debug.Log("[Mirror Migration Tool] Determined your asset folder is at: " + assetsFolder);
            Debug.Log("[Mirror Migration Tool] Scanning your C# scripts... This might take a moment.");

            // Now we scan the directory...
            try {
                DirectoryInfo dirInfo = new DirectoryInfo(assetsFolder);
                IEnumerable<FileInfo> potentialFiles = dirInfo.GetFiles(ScriptExtension, SearchOption.AllDirectories).Where(x => !x.DirectoryName.Contains(@"\Mirror\"));

                // For every entry in this structure add it to the list.
                // SearchOption.AllDirectories will traverse the directory stack
                foreach (FileInfo potentialFile in potentialFiles) {
                    // DEBUG ONLY. This will cause massive Unity Console Spammage!
                    // Debug.Log("[Mirror Migration Tool] DEBUG: Scanned " + potentialFile.FullName);
                    filesToScanAndModify.Add(potentialFile.FullName);
                }

                // Final chance to abort.
                if (!EditorUtility.DisplayDialog("Continue?", $"We've found {filesToScanAndModify.Count} file(s) that may need updating. Depending on your hardware and storage, " + "this might take a while. Do you wish to continue the process?", "Go ahead!", "Abort")) {
                    EditorUtility.DisplayDialog("Aborted", "You opted to abort the migration process. Please come back once you're ready to migrate.", "Got it");
                    return;
                }

                bool backupFiles = EditorUtility.DisplayDialog("Scripts Backup", 
                    "Do you want to backup each script which are going to be converted?\n" +
                    "If so, each script will be saved as .bak file. You can delete it later if needed.",
                    "Yes", "No");

                // Okay, let's do this!
                ProcessFiles(filesToScanAndModify, backupFiles);

                Debug.Log("[Mirror Migration Tool] Processed (and patched, if required) " + filesModified + " files");

                EditorUtility.DisplayDialog("Migration complete.", "Congratulations, you should now be Mirror Network ready.\n\n" +
                    "Thank you for using Mirror and Telepathy Networking Stack for Unity!\n\nPlease don't forget to drop by the GitHub " +
                    "repository to keep up to date and the Discord server if you have any problems. Have fun!", "Awesome");
            } catch (System.Exception ex) {
                EditorUtility.DisplayDialog("Oh no!", "An exception occurred. If you think this is a Mirror Networking bug, please file a bug report on the GitHub repository." +
                    "It could also be a logic bug in the Migration Tool itself. I encountered the following exception:\n\n" + ex.ToString(), "Okay");
                Cleanup();
            }
        }

        private static void ProcessFiles(List<string> filesToProcess, bool backupFiles) {
            StreamReader sr;
            StreamWriter sw;

            foreach (string file in filesToProcess) {
                try {
                    FileFormatting ff = LoadEditorConfig(file);
                    Encoding localencoding = ff.Encoding;
                    // Open and load it into the script buffer.
                    using (sr = new StreamReader(file, Utils.GetEncoding(file, localencoding))) {
                        scriptBuffer = sr.ReadToEnd();
                    }

                    if (scriptBuffer.Contains("//MirrorConverter NoConversion") || scriptBuffer.Contains("namespace Mirror")) continue;

                    // store initial buffer to use in final comparison before writing out file
                    string initialBuffer = scriptBuffer;
                    
                    if (scriptBuffer.Contains("using UnityEngine.Networking;")) {
                        foreach (string type in notUnetTypes) { 
                            if (scriptBuffer.Contains(type) && !scriptBuffer.Contains("using " + type)) {
                                int correctIndex = scriptBuffer.IndexOf("using UnityEngine.Networking;", StringComparison.Ordinal);
                                scriptBuffer = scriptBuffer.Insert(correctIndex, "using " +  type + " = UnityEngine.Networking." + type + ";" + ff.LineEnding);
                            }
                        }
                    }

                    scriptBuffer = scriptBuffer.Replace("using UnityEngine.Networking;", scriptBuffer.Contains("using Mirror;") ? "" : "using Mirror;");

                    // since [] characters are used by Regex, need to replace it by ourself
                    scriptBuffer = scriptBuffer.Replace("NetworkClient.allClients[0]", "NetworkClient");

                    // Work our magic.
                    for (int i = 0; i < knownIncompatibleRegexes.Length; i++) {
                        matches = Regex.Matches(scriptBuffer, knownIncompatibleRegexes[i]);
                        if (matches.Count > 0) {
                            // It was successful - replace it.
                            scriptBuffer = Regex.Replace(scriptBuffer, knownIncompatibleRegexes[i], knownCompatibleReplacements[i]);
                        }
                    }

                    // Be extra gentle with some like NetworkSettings directives.
                    matches = Regex.Matches(scriptBuffer, @"NetworkSettings\(([^\)]*)\)");
                    // A file could have more than one NetworkSettings... better to just do the whole lot.
                    // We don't know what the developer might be doing.
                    if (matches.Count > 0) {
                        for (int i = 0; i < matches.Count; i++) {
                            Match nsm = Regex.Match(matches[i].ToString(), @"(?<=\().+?(?=\))");
                            if (nsm.Success) {
                                string[] netSettingArguments = nsm.ToString().Split(',');
                                if (netSettingArguments.Length > 1) {
                                    string patchedNetSettings = string.Empty;

                                    int a = 0;
                                    foreach (string argument in netSettingArguments) {
                                        // Increment a, because that's how many elements we've looked at.
                                        a++;

                                        // If it contains the offender, just continue, don't do anything.
                                        if (argument.Contains("channel")) continue;

                                        // If it doesn't then add it to our new string.
                                        patchedNetSettings += argument.Trim();
                                        if (a < netSettingArguments.Length) patchedNetSettings += ", ";
                                    }

                                    // a = netSettingArguments.Length; patch it up and there we go.
                                    scriptBuffer = Regex.Replace(scriptBuffer, nsm.Value, patchedNetSettings);
                                } else {
                                    // Replace it.
                                    if (netSettingArguments[0].Contains("channel")) {
                                        // Don't touch this.
                                        scriptBuffer = scriptBuffer.Replace($"[{matches[i].Value}]", string.Empty);
                                    }
                                    // DONE!
                                }
                            }
                        }
                    }

                    // let comment SetChannelOption, since its not used anymore
                    matches = Regex.Matches(scriptBuffer, ".+SetChannelOption(.+)");
                    if (matches.Count > 0) {
                        foreach (Match match in matches) {
                            string newLine = "// ----- OBSOLETE | YOU CAN REMOVE THIS LINE (MIRROR) -----" + match.Value;
                            scriptBuffer = scriptBuffer.Replace(match.Value, newLine);
                        }
                    }

                    // Backup the old files for safety.
                    // The user can delete them later.
                    if (backupFiles && !File.Exists(file + ".bak"))
                        File.Copy(file, file + ".bak");

                    // Now the job is done, we want to write the data out to disk ONLY if the contents were actually changed... 
                    if (initialBuffer != scriptBuffer) {
                        using (sw = new StreamWriter(file, false, localencoding)) {
                            sw.Write(scriptBuffer.TrimStart());
                        }
                    }

                    // Increment the modified counter for statistics.
                    filesModified++;
                } catch (System.Exception e) {
                    // Kaboom, this tool ate something it shouldn't have.
                    Debug.LogError($"[Mirror Migration Tool] Encountered an exception processing {file}:\n{e.ToString()}");
                }
            }
        }

        static FileFormatting LoadEditorConfig(string filepath) {
            if (File.Exists(".editorconfig")) {
                EditorConfigParser parser = new EditorConfigParser();
                FileConfiguration[] configurations = parser.Parse(filepath).ToArray();
                FileConfiguration currentConfiguration = null;
                Encoding encoding = null;
                string intend = null;
                string lineEnding = null;
                switch (configurations.Length) {
                    case 0:
                        return new FileFormatting(Environment.NewLine, "    ", Utils.GetEncoding(filepath));
                    case 1:
                        currentConfiguration = configurations[0];
                        break;
                    default:
                        currentConfiguration = configurations[configurations.Length - 1];
                        break;
                }
                switch (currentConfiguration.Charset) {
                    case null:
                        encoding = Utils.GetEncoding(filepath);
                        break;
                    case Charset.Latin1:
                        encoding = Utils.Latin1Encoding;
                        break;
                    case Charset.UTF8:
                        encoding = Utils.Utf8NoBomEncoding;
                        break;
                    case Charset.UTF16BE:
                        encoding = Utils.Utf16BeBomEncoding;
                        break;
                    case Charset.UTF16LE:
                        encoding = Utils.Utf16LeBomEncoding;
                        break;
                    case Charset.UTF8BOM:
                        encoding = Utils.Utf8BomEncoding;
                        break;
                }

                switch (currentConfiguration.EndOfLine) {
                    case null:
                        lineEnding = Environment.NewLine;
                        break;
                    case EndOfLine.LF:
                        lineEnding = "\n";
                        break;
                    case EndOfLine.CRLF:
                        lineEnding = "\r\n";
                        break;
                    case EndOfLine.CR:
                        lineEnding = "\r";
                        break;
                }

                switch (currentConfiguration.IndentStyle) {
                    case null:
                        intend = "    ";
                        break;
                    case IndentStyle.Tab:
                        intend = "	";
                        break;
                    case IndentStyle.Space:
                        int? count = currentConfiguration.IndentSize.NumberOfColumns;
                        if (count != null) {
                            int spacecount = count.Value;
                            StringBuilder sb = new StringBuilder(spacecount, spacecount);
                            for (int i = 0; i < spacecount; i++) {
                                sb.Append(' ');
                            }

                            intend = sb.ToString();
                        }
                        else
                            intend = "    ";
                        break;
                }
                return new FileFormatting(lineEnding, intend, encoding);
            }

            return new FileFormatting(Environment.NewLine, "    ", Utils.GetEncoding(filepath));
        }

        /// <summary>
        /// Cleans up after the migration tool is completed or has failed.
        /// </summary>
        public static void Cleanup() {
            scriptBuffer = string.Empty;
            matches = null;
            filesModified = 0;
        }
    }

    readonly struct FileFormatting {
        public readonly string LineEnding;
        public readonly string Intendation;
        public readonly Encoding Encoding;

        public FileFormatting(string lineEnding, string intendation, Encoding encoding) {
            LineEnding = lineEnding;
            Intendation = intendation;
            Encoding = encoding;
        }
    }
}
