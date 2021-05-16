using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Shuttles
{
	public class MatrixPostProcessScene : MonoBehaviour
	{
		[PostProcessScene]
        public static void OnPostProcessScene()
        {
            // find all NetworkIdentities in all scenes
            // => can't limit it to GetActiveScene() because that wouldn't work
            //    for additive scene loads (the additively loaded scene is never
            //    the active scene)
            // => ignore DontDestroyOnLoad scene! this avoids weird situations
            //    like in NetworkZones when we destroy the local player and
            //    load another scene afterwards, yet the local player is still
            //    in the FindObjectsOfType result with scene=DontDestroyOnLoad
            //    for some reason
            // => OfTypeAll so disabled objects are included too
            // => Unity 2019 returns prefabs here too, so filter them out.

            IEnumerable<NetworkedMatrix> networkedMatrices = Resources.FindObjectsOfTypeAll<NetworkedMatrix>()
                .Where(identity => identity.gameObject.hideFlags != HideFlags.NotEditable &&
                                   identity.gameObject.hideFlags != HideFlags.HideAndDontSave &&
                                   identity.gameObject.scene.name != "DontDestroyOnLoad" &&
                                   !Utils.IsPrefab(identity.gameObject));

            foreach (NetworkedMatrix networkedMatrix in networkedMatrices)
            {

                // valid scene object?
                //   otherwise it might be an unopened scene that still has null
                //   sceneIds. builds are interrupted if they contain 0 sceneIds,
                //   but it's still possible that we call LoadScene in Editor
                //   for a previously unopened scene.
                //   (and only do SetActive if this was actually a scene object)
                if (networkedMatrix.networkedMatrixSceneId != 0)
                {
	                networkedMatrix.SetSceneIdSceneHashPartInternal();
                }
                // throwing an exception would only show it for one object
                // because this function would return afterwards.
                else
                {
                    // there are two cases where sceneId == 0:
                    // * if we have a prefab open in the prefab scene
                    // * if an unopened scene needs resaving
                    // show a proper error message in both cases so the user
                    // knows what to do.
                    string path = networkedMatrix.gameObject.scene.path;

                    if (string.IsNullOrWhiteSpace(path))
                    {
	                    Debug.LogError($"{networkedMatrix.name} is currently open in Prefab Edit Mode. Please open the actual scene before launching Mirror.");
                    }
                    else
                    {
	                    Debug.LogError($"Scene {path} needs to be opened and resaved, because the scene object {networkedMatrix.name} has no valid Networked Matrix sceneId yet.");
                    }
                }
            }
        }
	}
}
