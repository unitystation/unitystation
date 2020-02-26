using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles ChatBubbles and displays them in ScreenSpace
/// </summary>
public class ChatBubbleManager : MonoBehaviour
{
    private static ChatBubbleManager chatBubbleManager;
    public static ChatBubbleManager Instance{
	    get
	    {
		    if (chatBubbleManager == null)
		    {
			    chatBubbleManager = FindObjectOfType<ChatBubbleManager>();
		    }

		    return chatBubbleManager;
	    }
    }
    
    void Start()
    {
	    SceneManager.activeSceneChanged += OnSceneChange;
    }

    void OnDisable()
    {
	    SceneManager.activeSceneChanged -= OnSceneChange;
    }

    void OnSceneChange(Scene oldScene, Scene newScene)
    {

    }
}
