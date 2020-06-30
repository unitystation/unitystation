using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatEntry : MonoBehaviour
{
	[SerializeField] private Text visibleText = null;
	[SerializeField] private GameObject adminOverlay = null;
	[SerializeField] private Shadow shadow = null;
	[SerializeField] private RectTransform rectTransform = null;
	[SerializeField] private ContentSizeFitter contentFitter = null;
	[SerializeField] private LayoutElement layoutElement = null;
	[SerializeField] private List<Text> allText = new List<Text>();
	[SerializeField] private List<Image> allImages = new List<Image>();
	[SerializeField] private List<Button> allButtons = new List<Button>();
	public Transform thresholdMarkerBottom;
	public Transform thresholdMarkerTop;
	private Coroutine waitToCheck;


	/// <summary>
	/// The current message of the ChatEntry
	/// </summary>
	public string Message => visibleText.text;

	private bool isCoolingDown = true;
	public RectTransform rect;

	private Coroutine coCoolDown;
	private bool isHidden = false;
	private bool isAdminMsg = false;
	public GameObject stackTimesObj;
	public Text stackTimesText;
	private Image stackCircle;
	private int stackTimes = 1;
	private Vector3 localScaleCache;

	void Awake()
	{
		localScaleCache = stackTimesObj.transform.localScale;
	}

	void OnEnable()
	{
		stackCircle = stackTimesObj.GetComponent<Image>();
		EventManager.AddHandler(EVENT.ChatFocused, OnChatFocused);
		EventManager.AddHandler(EVENT.ChatUnfocused, OnChatUnfocused);
		ChatUI.Instance.scrollBarEvent += OnScrollInteract;
		ChatUI.Instance.checkPositionEvent += CheckPosition;
		if (!ChatUI.Instance.chatInputWindow.gameObject.activeInHierarchy)
		{
			if (isCoolingDown)
			{
				//coCoolDown = StartCoroutine(CoolDown());
			}
		}
	}

	void OnDisable()
	{
		EventManager.RemoveHandler(EVENT.ChatFocused, OnChatFocused);
		EventManager.RemoveHandler(EVENT.ChatUnfocused, OnChatUnfocused);
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
		isCoolingDown = false;
		isAdminMsg = false;
		visibleText.text = "";
		adminOverlay.SetActive(false);
		shadow.enabled = true;
		stackPosSet = false;
		stackTimes = 0;
		stackTimesText.text = "";
		stackTimesObj.SetActive(false);
		layoutElement.minHeight = 20f;
	}

	public void SetText(string msg)
	{
		visibleText.text = msg;
		ToggleUIElements(true);
		StartCoroutine(UpdateMinHeight());
	}

	IEnumerator UpdateMinHeight()
	{
		contentFitter.enabled = true;
		yield return WaitFor.EndOfFrame;
		layoutElement.minHeight = rectTransform.rect.height / 2;
		yield return WaitFor.EndOfFrame;
		contentFitter.enabled = false;
	}

	public void OnChatFocused()
	{
		// TODO Revisit fades in chat system v2
		/*
		if (isCoolingDown)
		{
			if (coCoolDown != null)
			{
				StopCoroutine(coCoolDown);
				coCoolDown = null;
			}
		}

		SetCrossFadeAlpha(1f, 0f);
		if (isHidden)
		{
			ToggleVisibleState(false);
		}
		*/
	}

	void CheckPosition()
	{
		if(waitToCheck != null) StopCoroutine(waitToCheck);
		waitToCheck = StartCoroutine(WaitToCheckPos());
	}

	IEnumerator WaitToCheckPos()
	{
		yield return WaitFor.EndOfFrame;

		// Get the chat entry's position outside of it's child position under ChatFeed.
		float entryYPositionOutsideChatFeedParent = transform.localPosition.y + transform.parent.localPosition.y;

		// Check to see if the chat entry is inside the VIEWPORT thresholds, and if so we will enable viewing it.
		if (entryYPositionOutsideChatFeedParent > thresholdMarkerBottom.localPosition.y && entryYPositionOutsideChatFeedParent < thresholdMarkerTop.localPosition.y) 
		{
			ToggleUIElements(true);
		}
		else
		{
			ToggleUIElements(false);
		}

		waitToCheck = null;
	}

	void ToggleVisibleState(bool hidden, bool fromCoolDown = false)
	{
		isHidden = hidden;
		ChatUI.Instance.ReportEntryState(hidden, fromCoolDown);
		if (fromCoolDown) return;
		ToggleUIElements(!hidden);
	}

	void ToggleUIElements(bool enabled)
	{
		shadow.enabled = enabled;

		foreach (var t in allText)
		{
			t.enabled = enabled;
		}

		foreach (var i in allImages)
		{
			i.enabled = enabled;
		}

		foreach (var b in allButtons)
		{
			b.enabled = enabled;
		}

		if (enabled && isAdminMsg)
		{
			shadow.enabled = false;
		}
	}

	public void OnChatUnfocused()
	{
		// TODO Revisit fades in chat system v2
		/*
		if (isCoolingDown)
		{
			if (coCoolDown != null) StopCoroutine(coCoolDown);

			//coCoolDown = StartCoroutine(CoolDown());
		}
		else
		{
			SetCrossFadeAlpha(0f, 0f);
			if (!isHidden)
			{
				ToggleVisibleState(true);
			}
		}
		*/
	}

//	IEnumerator CoolDown()
//	{
//		Vector2 sizeDelta = rect.sizeDelta;
//		sizeDelta.x = 472f;
//		rect.sizeDelta = sizeDelta;
//		yield return WaitFor.EndOfFrame;
//		SetStackPos();
//		yield return WaitFor.Seconds(12f);
//		bool toggleVisibleState = false;
//		if (!ChatUI.Instance.chatInputWindow.gameObject.activeInHierarchy)
//		{
//			SetCrossFadeAlpha(0.01f, 3f);
//			if (!isHidden)
//			{
//				ToggleVisibleState(true, true);
//				toggleVisibleState = true;
//			}
//		}
//		else
//		{
//			yield break;
//		}
//
//		yield return WaitFor.Seconds(3f);
//
//		if (toggleVisibleState)
//		{
//			ToggleUIElements(false);
//		}
//
//		isCoolingDown = false;
//	}

	public void AddChatDuplication()
	{
		stackTimes++;
		// TODO Switched off until we do ChatSystem V2
		/*
		stackTimesText.text = $"x{stackTimes}";
		stackTimesObj.SetActive(true);
		StartCoroutine(StackPumpAnim());
		SetCrossFadeAlpha(1f, 0f);
		if (isCoolingDown)
		{
			if (coCoolDown != null) StopCoroutine(coCoolDown);
			//coCoolDown = StartCoroutine(CoolDown());
		}
		else
		{
			isCoolingDown = true;
			//coCoolDown = StartCoroutine(CoolDown());
		}
		*/
	}

	IEnumerator StackPumpAnim()
	{
		yield return WaitFor.EndOfFrame;
		var targetScale = localScaleCache * 1.1f;
		var progress = 0f;
		while (progress < 1f)
		{
			progress += Time.deltaTime * 10f;
			stackTimesObj.transform.localScale = Vector3.Lerp(localScaleCache, targetScale, progress);
			yield return WaitFor.EndOfFrame;
		}

		progress = 0f;
		while (progress < 1f)
		{
			progress += Time.deltaTime * 10f;
			stackTimesObj.transform.localScale = Vector3.Lerp(targetScale, localScaleCache, progress);
			yield return WaitFor.EndOfFrame;
		}

		stackTimesObj.transform.localScale = localScaleCache;
	}

	//state = is mouse button down on the scroll bar handle
	private void OnScrollInteract(bool state)
	{
		if (isHidden && state)
		{
			SetCrossFadeAlpha(1f, 0f);
			ToggleVisibleState(false);
		}

//		if (!isHidden && !state
//		              && !ChatUI.Instance.chatInputWindow.gameObject.activeInHierarchy)
//		{
//			if (isCoolingDown)
//			{
//				if (coCoolDown != null)
//				{
//					StopCoroutine(coCoolDown);
//					coCoolDown = null;
//				}
//			}
//
//			isCoolingDown = true;
//			if (gameObject.activeInHierarchy)
//			{
//				coCoolDown = StartCoroutine(CoolDown());
//			}
//			else
//			{
//				isCoolingDown = false;
//			}
//		}
	}

	private bool stackPosSet = false;

	void SetStackPos()
	{
		if (string.IsNullOrEmpty(visibleText.text)) return;

		if (stackPosSet) return;
		stackPosSet = true;

		string _text = visibleText.text;

		TextGenerator textGen = new TextGenerator(_text.Length);
		Vector2 extents = visibleText.gameObject.GetComponent<RectTransform>().rect.size;
		textGen.Populate(_text, visibleText.GetGenerationSettings(extents));
		if (textGen.vertexCount == 0)
		{
			return;
		}

		var newPos = stackTimesObj.transform.localPosition;
		newPos.x = (textGen.verts[textGen.vertexCount - 1].position / visibleText.canvas.scaleFactor).x;


		if (rect.rect.height < 30f)
		{
			newPos.y += 3.5f * rect.localScale.y;
		}

		stackTimesObj.transform.localPosition = newPos;
	}

	void SetCrossFadeAlpha(float amt, float time)
	{
		// TODO Revisit fades in chat system v2
		/*
		return;
		visibleText.CrossFadeAlpha(amt, time, false);
		stackTimesText.CrossFadeAlpha(amt, time, false);
		stackCircle.CrossFadeAlpha(amt, time, false);
		*/
	}
}