using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;
using TMPro;


/// <summary>
/// <para>Responsible for managing the invidividual chat entry in the chat UI.</para>
/// <remarks>Try to keep it lightweight.</remarks>
/// </summary>
public class ChatEntry : MonoBehaviour
{
	[SerializeField, BoxGroup("Entry Object")]
	private RectTransform entryTransform;
	[SerializeField, BoxGroup("Entry Object")]
	private LayoutElement entryLayoutElement = default;

	[SerializeField, BoxGroup("Message Object")]
	private RectTransform messageTransform = default;
	[SerializeField, BoxGroup("Message Object")]
	private TMP_Text messageText = default;
	[SerializeField, BoxGroup("Message Object")]
	private ContentSizeFitter messageContentFitter = default;

	[SerializeField, BoxGroup("Stack Object")]
	private GameObject stackObject = default;
	[SerializeField, BoxGroup("Stack Object")]
	private TMP_Text stackText = default;
	[SerializeField, BoxGroup("Stack Object")]
	private Image stackImage = default;

	[NonSerialized]
	public Transform thresholdMarkerBottom;
	[NonSerialized]
	public Transform thresholdMarkerTop;

	/// <summary>The current message of the <see cref="ChatEntry"/>.</summary>
	public string Message => messageText.text;

	private bool IsChatFocused => ChatUI.Instance.chatInputWindow.gameObject.activeInHierarchy;

	private Coroutine waitToCheck;
	private Coroutine fadeCooldownCoroutine;
	private bool isHidden = false;

	private int stackCount = 1;
	private Vector3 stackScaleCache;

	#region Lifecycle

	private void Awake()
	{
		stackScaleCache = stackObject.transform.localScale;
	}

	private void OnEnable()
	{
		EventManager.AddHandler(Event.ChatFocused, OnChatFocused);
		EventManager.AddHandler(Event.ChatUnfocused, OnChatUnfocused);
		ChatUI.Instance.scrollBarEvent += OnScrollInteract;
		ChatUI.Instance.checkPositionEvent += CheckPosition;
		if (IsChatFocused == false)
		{
			this.RestartCoroutine(FadeCooldown(), ref fadeCooldownCoroutine);
		}
	}

	private void OnDisable()
	{
		EventManager.RemoveHandler(Event.ChatFocused, OnChatFocused);
		EventManager.RemoveHandler(Event.ChatUnfocused, OnChatUnfocused);
		if (ChatUI.Instance != null)
		{
			ChatUI.Instance.scrollBarEvent -= OnScrollInteract;
			ChatUI.Instance.checkPositionEvent -= CheckPosition;
		}
	}

	public void ReturnToPool()
	{
		if (ChatUI.Instance != null)
		{
			if (isHidden)
			{
				ChatUI.Instance.ReportEntryState(false);
			}

			ResetEntry();
			ChatUI.Instance.entryPool.ReturnChatEntry(gameObject);
		}
	}

	private void ResetEntry()
	{
		messageText.text = "";
		stackText.text = "";
		stackCount = 1;
		stackObject.SetActive(false);
		entryLayoutElement.minHeight = 20f;
		messageText.raycastTarget = false;
		AnimateFade(1f, 0f);
	}

	#endregion

	#region Events

	public void OnChatFocused()
	{
		this.TryStopCoroutine(ref fadeCooldownCoroutine);

		AnimateFade(1f, 0f);
		if (isHidden)
		{
			SetHidden(false);
		}
	}

	public void OnChatUnfocused()
	{
		this.RestartCoroutine(FadeCooldown(), ref fadeCooldownCoroutine);
	}

	private void OnScrollInteract(bool isScrolling)
	{
		// isScrolling = is mouse button down on the scroll bar handle

		if (isHidden && isScrolling)
		{
			AnimateFade(1f, 0f);
			SetHidden(false);
		}

		if (!isHidden && !isScrolling && !IsChatFocused)
		{
			this.RestartCoroutine(FadeCooldown(), ref fadeCooldownCoroutine);
		}
	}

	private void CheckPosition()
	{
		this.RestartCoroutine(WaitToCheckPos(), ref waitToCheck);
	}

	#endregion

	public void SetText(string message)
	{
		messageText.text = message;
		ToggleUIElements(true);
		StartCoroutine(UpdateMinHeight());

		if (message.Contains("</link>"))
		{
			messageText.raycastTarget = true;
		}
	}


	public void AddChatDuplication()
	{
		if (stackCount == 1)
		{
			// Just need to do this once; message size won't change.
			SetStackPos();
		}

		stackText.text = $"x{++stackCount}";
		ToggleUIElements(true);
		stackObject.SetActive(true);
		AnimateFade(1f, 0f);
		StartCoroutine(AnimateStackObject());
		this.RestartCoroutine(FadeCooldown(), ref fadeCooldownCoroutine);
	}

	private void SetHidden(bool hidden, bool fromCooldown = false)
	{
		isHidden = hidden;
		ChatUI.Instance.ReportEntryState(hidden, fromCooldown);
		if (fromCooldown) return;
		ToggleUIElements(!hidden);
	}

	private void ToggleUIElements(bool enabled)
	{
		messageText.enabled = enabled;
		stackText.enabled = enabled;
		stackImage.enabled = enabled;
	}

	private IEnumerator UpdateMinHeight()
	{
		// I think we disable the content fitter when not needed for performance
		messageContentFitter.enabled = true;
		yield return WaitFor.EndOfFrame;
		entryLayoutElement.minHeight = messageTransform.rect.height / 2;
		yield return WaitFor.EndOfFrame;
		messageContentFitter.enabled = false;
	}

	private IEnumerator WaitToCheckPos()
	{
		yield return WaitFor.EndOfFrame;

		// Get the chat entry's position outside of its child position under ChatFeed.
		float entryYPositionOutsideChatFeedParent = transform.localPosition.y + transform.parent.localPosition.y;

		// Check to see if the chat entry is inside the VIEWPORT thresholds, and if so we will enable viewing it.
		bool isInsideViewport = entryYPositionOutsideChatFeedParent > thresholdMarkerBottom.localPosition.y;
		isInsideViewport &= entryYPositionOutsideChatFeedParent < thresholdMarkerTop.localPosition.y;
		if (isHidden == false)
		{
			ToggleUIElements(isInsideViewport);
		}

		waitToCheck = null;
	}

	private IEnumerator FadeCooldown()
	{
		yield return WaitFor.Seconds(12f);
		bool toggleVisibleState = false;

		// Chat may have become focused during this time. Don't fade away if now focused.
		if (IsChatFocused) yield break;

		AnimateFade(0.01f, 3f);
		if (isHidden == false)
		{
			SetHidden(true, true);
			toggleVisibleState = true;
		}

		yield return WaitFor.Seconds(3f);

		if (toggleVisibleState)
		{
			ToggleUIElements(false);
		}
	}

	private IEnumerator AnimateStackObject()
	{
		yield return WaitFor.EndOfFrame;
		var scaleFactor = Mathf.Min(0.1f * (stackCount - 1), 2);
		var targetScale = new Vector3(stackScaleCache.x + scaleFactor, stackScaleCache.y + scaleFactor, 1);
		var progress = 0f;
		while (progress < 1f)
		{
			progress += Time.deltaTime * 10f;
			stackObject.transform.localScale = Vector3.Lerp(stackScaleCache, targetScale, progress);
			yield return WaitFor.EndOfFrame;
		}

		progress = 0f;
		while (progress < 1f)
		{
			progress += Time.deltaTime * 10f;
			stackObject.transform.localScale = Vector3.Lerp(targetScale, stackScaleCache, progress);
			yield return WaitFor.EndOfFrame;
		}

		stackObject.transform.localScale = stackScaleCache;
	}

	private void SetStackPos()
	{
		if (messageText.textInfo.characterCount - 1 < messageText.textInfo.characterInfo.Length)
		{
			var lastCharacter = messageText.textInfo.characterInfo[messageText.textInfo.characterCount - 1];
			var charWorld = messageText.transform.TransformPoint(lastCharacter.bottomRight);
			var newWorldPos = stackObject.transform.position;
			newWorldPos.x = charWorld.x + 3;
			stackObject.transform.position = newWorldPos;
		}
		else
		{
			Logger.LogError("Caught an IndexOutOfRange in ChatEntry.SetStackPos() ln 273", Category.Chat);
		}
	}

	private void AnimateFade(float toAlpha, float time)
	{
		messageText.CrossFadeAlpha(toAlpha, time, false);
		stackText.CrossFadeAlpha(toAlpha, time, false);
		stackImage.CrossFadeAlpha(toAlpha, time, false);
	}
}
