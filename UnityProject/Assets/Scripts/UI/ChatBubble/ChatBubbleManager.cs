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
	    if (!PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubbleSize))
	    {
		    PlayerPrefs.SetFloat(PlayerPrefKeys.ChatBubbleSize, 2f);
		    PlayerPrefs.Save();
	    }
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

    /// <summary>
    /// Display a chat bubble and make it follow a transform target
    /// </summary>
    /// <param name="msg">Text to show in the chat bubble</param>
    /// <param name="followTarget">The transform in the world for the bubble to follow</param>
    /// <param name="chatModifier">Any chat modifiers that need to be applied</param>
    public static void ShowAChatBubble(Transform followTarget, string msg,
	    ChatModifier chatModifier = ChatModifier.None)
    {
		Instance.GetChatBubbleFromPool().SetupBubble(followTarget, msg, chatModifier);
    }

    ChatBubble GetChatBubbleFromPool()
    {
	    var index = chatBubblePool.FindIndex(x => !x.gameObject.activeInHierarchy);

	    if (index != -1)
	    {
		    return chatBubblePool[index];
	    }
	    else
	    {
		    var newBubble = SpawnNewChatBubble();
		    chatBubblePool.Add(newBubble);
		    return newBubble;
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
