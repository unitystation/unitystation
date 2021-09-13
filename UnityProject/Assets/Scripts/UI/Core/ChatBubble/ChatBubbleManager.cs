using System.Collections;
using System.Collections.Generic;
using Initialisation;
using Managers.SettingsManager;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles ChatBubbles and displays them in ScreenSpace
/// </summary>
public class ChatBubbleManager : MonoBehaviour, IInitialise
{
	private static ChatBubbleManager chatBubbleManager;

	public static ChatBubbleManager Instance
	{
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
	private List<ActionText> actionPool = new List<ActionText>();
	[SerializeField] private GameObject chatBubblePrefab = null;
	[SerializeField] private GameObject ActionPrefab = null;
	[SerializeField] private int initialPoolSize = 10;


	public InitialisationSystems Subsystem => InitialisationSystems.ChatBubbleManager;

	void IInitialise.Initialise()
	{
		if (PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubbleSize) == false)
		{
			PlayerPrefs.SetFloat(PlayerPrefKeys.ChatBubbleSize, DisplaySettings.DEFAULT_CHATBUBBLESIZE);
			PlayerPrefs.Save();
		}

		if (PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubbleInstant) == false)
		{
			PlayerPrefs.SetInt(PlayerPrefKeys.ChatBubbleInstant, DisplaySettings.DEFAULT_CHATBUBBLEINSTANT);
			PlayerPrefs.Save();
		}

		if (PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubblePopInSpeed) == false)
		{
			PlayerPrefs.SetFloat(PlayerPrefKeys.ChatBubblePopInSpeed, DisplaySettings.DEFAULT_CHATBUBBLEPOPINSPEED);
			PlayerPrefs.Save();
		}

		if (PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubbleAdditionalTime) == false)
		{
			PlayerPrefs.SetFloat(PlayerPrefKeys.ChatBubbleAdditionalTime, DisplaySettings.DEFAULT_CHATBUBBLEADDITIONALTIME);
			PlayerPrefs.Save();
		}

		if (PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubbleClownColour) == false)
		{
			PlayerPrefs.SetInt(PlayerPrefKeys.ChatBubbleClownColour, DisplaySettings.DEFAULT_CHATBUBBLECLOWNCOLOUR);
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
		//TODO this will prevent emotes from appearing as speech. We should streamline it and simply don't use
		// the chat api when the message is an emote, instead generate an action message.
		if ((chatModifier & ChatModifier.Emote) == ChatModifier.Emote)
		{
			return;
		}

		var index = Instance.chatBubblePool.FindIndex(x => x.Target == followTarget);

		if (index != -1)
		{
			if (Instance.chatBubblePool[index].gameObject.activeInHierarchy)
			{
				Instance.chatBubblePool[index].AppendToBubble(msg, chatModifier);
				return;
			}
		}

		Instance.GetChatBubbleFromPool().SetupBubble(followTarget, msg, chatModifier);
	}


	/// <summary>
	/// Display a chat bubble and make it follow a transform target
	/// </summary>
	/// <param name="msg">Text to show in the Action</param>
	public void ShowAction(string msg, GameObject recipient)
	{
		var index = actionPool.FindIndex(x => x.Text.text == msg);

		if (index != -1)
		{
			actionPool[index].AddMultiplier();
			return;
		}
		GetChatBubbleActionText().SetUp(msg, recipient);
	}


	ActionText GetChatBubbleActionText()
	{
		var index = actionPool.FindIndex(x => x.OrNull()?.gameObject.activeInHierarchy == false);

		if (index != -1)
		{
			return actionPool[index];
		}
		else
		{
			var newBubble = SpawnNewActionText();
			actionPool.Add(newBubble);
			return newBubble;
		}
	}

	ActionText SpawnNewActionText()
	{
		var obj = Instantiate(ActionPrefab, Vector3.zero, Quaternion.identity);
		obj.transform.SetParent(transform, false);
		obj.SetActive(false);
		return obj.GetComponent<ActionText>();
	}


	ChatBubble GetChatBubbleFromPool()
	{
		var index = chatBubblePool.FindIndex(x => x.gameObject.OrNull()?.activeInHierarchy == false);

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
		obj.transform.SetParent(transform,
			false); // Suggestion by compiler, instead of obj.transform.parent = transform;
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