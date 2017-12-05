using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles the update methods for in game objects
/// Handling the updates from a single point decreases cpu time
/// and increases performance
/// </summary>
public class UpdateManager : MonoBehaviour
{
    private static UpdateManager updateManager;
    public static UpdateManager Instance
    {
        get
        {
            if (updateManager == null)
            {
                updateManager = FindObjectOfType<UpdateManager>();
            }
            return updateManager;
        }
    }

    //List of all the objects to override UpdateMe method in Update
    public List<ManagedNetworkBehaviour> regularUpdate = new List<ManagedNetworkBehaviour>();

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += SceneChanged;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= SceneChanged;
    }

    private void SceneChanged(Scene prevScene, Scene newScene)
    {
        Reset();
    }

    // Reset the references when the scene is changed
    private void Reset()
    {
        regularUpdate.Clear();
    }

    private void Update()
    {
        for (int i = 0; i < regularUpdate.Count; i++)
        {
            regularUpdate[i].UpdateMe();
            regularUpdate[i].FixedUpdateMe();
            regularUpdate[i].LateUpdateMe();
        }
    }


}











