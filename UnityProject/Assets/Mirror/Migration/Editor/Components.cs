#pragma warning disable 0618
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnetNetworkAnimator = UnityEngine.Networking.NetworkAnimator;
using UnetNetworkIdentity = UnityEngine.Networking.NetworkIdentity;
using UnetNetworkManager = UnityEngine.Networking.NetworkManager;
using UnetNetworkProximityChecker = UnityEngine.Networking.NetworkProximityChecker;
using UnetNetworkStartPosition = UnityEngine.Networking.NetworkStartPosition;
using UnetNetworkTransform = UnityEngine.Networking.NetworkTransform;
using UnetNetworkTransformChild = UnityEngine.Networking.NetworkTransformChild;
using UnetNetworkLobbyManager = UnityEngine.Networking.NetworkLobbyManager;
using UnetNetworkLobbyPlayer = UnityEngine.Networking.NetworkLobbyPlayer;
using UnetNetworkManagerHUD = UnityEngine.Networking.NetworkManagerHUD;

using UnetNetworkDiscovery = UnityEngine.Networking.NetworkDiscovery;

using MirrorNetworkAnimator = Mirror.NetworkAnimator;
using MirrorNetworkIdentity = Mirror.NetworkIdentity;
using MirrorNetworkManager = Mirror.NetworkManager;
using MirrorNetworkProximityChecker = Mirror.NetworkProximityChecker;
using MirrorNetworkStartPosition = Mirror.NetworkStartPosition;
using MirrorNetworkTransform = Mirror.NetworkTransform;
using MirrorNetworkTransformChild = Mirror.NetworkTransformChild;
using MirrorNetworkLobbyManager = Mirror.NetworkLobbyManager;
using MirrorNetworkLobbyPlayer = Mirror.NetworkLobbyPlayer;
using MirrorNetworkManagerHUD = Mirror.NetworkManagerHUD;
using UnityEngine.SceneManagement;

namespace Mirror.MigrationUtilities {
    public class Components : MonoBehaviour {

        public static void FindAndReplaceUnetComponents(out int netComponentObsolete) {
            int fileCounter = 0; // files on the project
            netComponentObsolete = 0; // obsolete components found (like lobby)
            int netIdComponentsCount = 0; // network identities
            int netComponentCount = 0; // networking components
            string logErrors = ""; // error message

            string[] files = Directory.GetFiles(Application.dataPath, "*.prefab", SearchOption.AllDirectories);
            int gameObjectCount = files.Length;

            try {
                foreach (string file in files) {
                    fileCounter++;
                    int numChangesOnFile = 0;
                    string relativepath = "Assets" + file.Substring(Application.dataPath.Length);

                    EditorUtility.DisplayProgressBar("Mirror Migration Progress", $"{fileCounter} of {gameObjectCount} files scanned...", fileCounter / gameObjectCount);

                    GameObject prefab;
                    try {
                        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(relativepath);
                    } catch {
                        continue;
                    }

                    if (prefab == null)
                        continue;

                    IEnumerable<Transform> childsAndParent = prefab.GetComponentsInChildren<Transform>(true);

                    foreach (Transform actualChild in childsAndParent) {
                        // replace UNET components with their mirror counterpart
                        int numNetworkComponentChanges = ReplaceEveryNetworkComponent(actualChild.gameObject);
                        numChangesOnFile += numNetworkComponentChanges;
                        netComponentCount += numNetworkComponentChanges;

                        // always replace NetworkIdentity as last element, due to dependencies
                        if (ReplaceEveryNetworkIdentity(actualChild.gameObject)) {
                            numChangesOnFile++;
                            netIdComponentsCount++;
                        }

                        // check for obsolete components
                        logErrors += CheckObsoleteComponents(actualChild.gameObject, out int compObsolete);
                        netComponentObsolete += compObsolete;

                        // remove missing components (mono scripts)
                        if (numChangesOnFile >= 1) {
#if UNITY_2019_1_OR_NEWER
                            int removedComponentCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(actualChild.gameObject);
#else
                            int removedComponentCount = RemoveMissingComponents(actualChild.gameObject);
#endif
                            if (removedComponentCount > 0)
                                Debug.LogError("Had to remove " + removedComponentCount + " missing components in the following prefab:" + actualChild.gameObject.name + " otherwise it's impossible to save it.");
                        }
                    }

                    if (numChangesOnFile >= 1)
                        PrefabUtility.ReplacePrefab(prefab, AssetDatabase.LoadAssetAtPath<Object>(prefab.name + ".prefab"));
                }
                Debug.LogFormat("Searched {0} Prefabs, found {1} UNET NetworkIdentity, {2} Components and replaced them with Mirror components.\nAlso found {3} now deprecated components.", gameObjectCount, netIdComponentsCount, netComponentCount, netComponentObsolete);

                if (netComponentObsolete > 0)
                    Debug.LogWarningFormat("List of now deprecated components found on your project:\n{0}", logErrors);
            } catch (System.Exception e) {
                Debug.LogError("[Mirror Migration Tool] Encountered an exception!");
                Debug.LogException(e);
            } finally {
                EditorUtility.ClearProgressBar();
            }
        }

        public static void FindAndReplaceUnetSceneGameObject(out int netComponentObsolete) {
            int convertedGoCounter = 0; // counter of converted game objects
            netComponentObsolete = 0; // obsolete components found (like lobby)
            int netIdComponentsCount = 0; // network identities
            int netComponentCount = 0; // networking components
            string logErrors = ""; // error message

            // safest way to get all gameObjects on the scene instead of FindObjectOfType()
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] allObjetsInScene = allObjects.Where(x => x.scene == activeScene).ToArray();

            int gameObjectCount = allObjetsInScene.Length;

            foreach (GameObject currentGameObject in allObjetsInScene) {
                if (currentGameObject.hideFlags == HideFlags.NotEditable || currentGameObject.hideFlags == HideFlags.HideAndDontSave)
                    continue;

                convertedGoCounter++;
                EditorUtility.DisplayProgressBar("Mirror Migration Progress", $"{convertedGoCounter} of {gameObjectCount} game object scanned...", convertedGoCounter / gameObjectCount);

                IEnumerable<Transform> childsAndParent = currentGameObject.GetComponentsInChildren<Transform>(true);

                foreach (Transform actualChild in childsAndParent) {
                    // replace UNET components with their mirror counterpart
                    netComponentCount += ReplaceEveryNetworkComponent(actualChild.gameObject);

                    // always replace NetworkIdentity as last element, due to dependencies
                    if (ReplaceEveryNetworkIdentity(actualChild.gameObject)) netIdComponentsCount++;

                    // check for obsolete components
                    logErrors += CheckObsoleteComponents(actualChild.gameObject, out int compObsolete);
                    netComponentObsolete += compObsolete;
                }
            }

            EditorUtility.ClearProgressBar();
            Debug.LogFormat("Searched {0} GameObjects, found {1} UNET NetworkIdentity, {2} Components and replaced them with Mirror components.\nAlso found {3} now deprecated components.", convertedGoCounter, netIdComponentsCount, netComponentCount, netComponentObsolete);

            if (netComponentObsolete > 0)
                Debug.LogWarningFormat("List of now deprecated components found on your project:\n {0}", logErrors);
        }

        static int ReplaceEveryNetworkComponent(GameObject go) {
            int compCount = 0;

            if (Utils.ReplaceNetworkComponent<UnetNetworkAnimator, MirrorNetworkAnimator>(go)) compCount++;
            if (Utils.ReplaceNetworkComponent<UnetNetworkTransform, MirrorNetworkTransform>(go)) compCount++;
            if (Utils.ReplaceNetworkComponent<UnetNetworkManagerHUD, MirrorNetworkManagerHUD>(go)) compCount++;
            if (Utils.ReplaceNetworkComponent<UnetNetworkManager, MirrorNetworkManager>(go)) compCount++;
            if (Utils.ReplaceNetworkComponent<UnetNetworkProximityChecker, MirrorNetworkProximityChecker>(go)) compCount++;
            if (Utils.ReplaceNetworkComponent<UnetNetworkStartPosition, MirrorNetworkStartPosition>(go)) compCount++;
            if (Utils.ReplaceNetworkComponent<UnetNetworkTransformChild, MirrorNetworkTransformChild>(go)) compCount++;
            if (Utils.ReplaceNetworkComponent<UnetNetworkLobbyManager, MirrorNetworkLobbyManager>(go)) compCount++;
            if (Utils.ReplaceNetworkComponent<UnetNetworkLobbyPlayer, MirrorNetworkLobbyPlayer>(go)) compCount++;

            return compCount;
        }

        static bool ReplaceEveryNetworkIdentity(GameObject go) {
            return Utils.ReplaceNetworkIdentity(go);
        }

        static string CheckObsoleteComponents(GameObject go, out int compObsolete) {
            string errors = "";
            compObsolete = 0;

            // TODO: others obsolete components from original UNET (and not HLAPI_CE)
            if (go.GetComponent<UnetNetworkDiscovery>()) {
                compObsolete++;
                errors += go.name + "\n";
            }

            return errors;
        }

        // remove ALL missing components from a gameobject, otherwise we can't save it as a prefab
        static int RemoveMissingComponents(GameObject go) {
            SerializedObject serializedChild = new SerializedObject(go);
            SerializedProperty serializedComponent = serializedChild.FindProperty("m_Component");
            Component[] components = go.GetComponents<Component>();
            int removedComponentCount = 0;

            for (int i = 0; i < components.Length; i++) {
                if (components[i] == null) {
                    serializedComponent.DeleteArrayElementAtIndex(i - removedComponentCount);
                    ++removedComponentCount;
                } 
            }

            serializedChild.ApplyModifiedPropertiesWithoutUndo();
            return removedComponentCount;
        }
    }
}
