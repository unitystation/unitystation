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

    private List<ChatBubble> chatBubblePool = new List<ChatBubble>();
    [SerializeField] private GameObject chatBubblePrefab;
    [SerializeField] private int initialPoolSize = 10;

    void Start()
    {
	    SceneManager.activeSceneChanged += OnSceneChange;
	    StartCoroutine(InitCache());
    }

    IEnumerator InitCache()
    {
	    while (chatBubblePool.Count < initialPoolSize)
	    {
		    chatBubblePool.Add(SpawnNewChatBubble());
		    yield return WaitFor.EndOfFrame;
	    }
    }

    ChatBubble SpawnNewChatBubble()
    {
	    var obj = Instantiate(chatBubblePrefab, Vector3.zero, Quaternion.identity);
	    obj.transform.parent = transform;
	    obj.transform.localScale = Vector3.one * 2f;
	    obj.SetActive(false);
	    return obj.GetComponent<ChatBubble>();
    }

    void OnDisable()
    {
	    SceneManager.activeSceneChanged -= OnSceneChange;
    }

    void OnSceneChange(Scene oldScene, Scene newScene)
    {
		ResetAll();
    }

    void ResetAll()
    {
	    foreach (var cb in chatBubblePool)
	    {
		    if (cb.gameObject.activeInHierarchy)
		    {
			    cb.ReturnToPool();
		    }
	    }
    }
}
