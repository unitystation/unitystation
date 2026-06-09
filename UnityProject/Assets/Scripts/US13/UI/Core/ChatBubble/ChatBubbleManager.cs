using System;
using System.Collections;
using System.Collections.Generic;
using Shared.Util;
using UnityEngine;
using UnityEngine.SceneManagement;
using US13.Core.Chat;
using US13.Core.Initialisation;
using US13.Managers;
using US13.Managers.SettingsManager;
using US13.PlayerPrefs;
using Util;
using Event = US13.Managers.Event;

namespace US13.UI.Core.ChatBubble
{
	/// <summary>
	/// Handles ChatBubbles and displays them in ScreenSpace
	/// </summary>
	public class ChatBubbleManager : MonoBehaviour, IInitialise, IDisposable
	{
		private static ChatBubbleManager chatBubbleManager;

		public static ChatBubbleManager Instance => FindUtils.LazyFindObject(ref chatBubbleManager);

		private List<ChatBubble> chatBubblePool = new List<ChatBubble>();
		private List<ActionText> actionPool = new List<ActionText>();
		[SerializeField] private GameObject chatBubblePrefab = null;
		[SerializeField] private GameObject ActionPrefab = null;
		[SerializeField] private int initialPoolSize = 10;


		public InitialisationSystems Subsystem => InitialisationSystems.ChatBubbleManager;

		public void Clear()
		{
			chatBubblePool.Clear();
			actionPool.Clear();
		}

		void IInitialise.Initialise()
		{
			if (UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubbleSize) == false)
			{
				UnityEngine.PlayerPrefs.SetFloat(PlayerPrefKeys.ChatBubbleSize, DisplaySettings.DEFAULT_CHATBUBBLESIZE);
				UnityEngine.PlayerPrefs.Save();
			}

			if (UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubblePopInSpeed) == false)
			{
				UnityEngine.PlayerPrefs.SetFloat(PlayerPrefKeys.ChatBubblePopInSpeed, DisplaySettings.DEFAULT_CHATBUBBLEPOPINSPEED);
				UnityEngine.PlayerPrefs.Save();
			}

			if (UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubbleAdditionalTime) == false)
			{
				UnityEngine.PlayerPrefs.SetFloat(PlayerPrefKeys.ChatBubbleAdditionalTime, DisplaySettings.DEFAULT_CHATBUBBLEADDITIONALTIME);
				UnityEngine.PlayerPrefs.Save();
			}

			if (UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubbleClownColour) == false)
			{
				UnityEngine.PlayerPrefs.SetInt(PlayerPrefKeys.ChatBubbleClownColour, DisplaySettings.DEFAULT_CHATBUBBLECLOWNCOLOUR);
				UnityEngine.PlayerPrefs.Save();
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

		private void OnEnable()
		{
			EventManager.AddHandler(Event.SceneUnloading, ChatBubbleManager.Instance.Clear);
		}

		private void OnDisable()
		{
			EventManager.RemoveHandler(Event.SceneUnloading, ChatBubbleManager.Instance.Clear);
			SceneManager.activeSceneChanged -= OnSceneChange;
		}

		void OnDestroy()
		{
			this.Dispose();
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

		public void Dispose()
		{
			foreach (ChatBubble chatBubble in chatBubblePool)
			{
				chatBubble.OrNull()?.Dispose();
			}
		}
	}
}