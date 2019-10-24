using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChatEntry : MonoBehaviour
{
	public Text text;
	private bool isCoolingDown = true;
	public RectTransform rect;

	private Coroutine coCoolDown;
	private bool isHidden = false;

	void OnEnable()
	{
		EventManager.AddHandler(EVENT.ChatFocused, OnChatFocused);
		EventManager.AddHandler(EVENT.ChatUnfocused, OnChatUnfocused);
		ChatUI.Instance.scrollBarEvent += OnScrollInteract;
		if (!ChatUI.Instance.chatInputWindow.gameObject.activeInHierarchy)
		{
			if (isCoolingDown)
			{
				coCoolDown = StartCoroutine(CoolDown());
			}
		}
	}

	void OnDisable()
	{
		EventManager.RemoveHandler(EVENT.ChatFocused, OnChatFocused);
		EventManager.RemoveHandler(EVENT.ChatUnfocused, OnChatUnfocused);
	}

	public void OnDestroy()
	{
		if (ChatUI.Instance != null)
		{
			ChatUI.Instance.scrollBarEvent -= OnScrollInteract;
			if (isHidden)
			{
				ChatUI.Instance.ReportEntryState(false);
			}
		}
	}

	public void OnChatFocused()
	{
		if (isCoolingDown)
		{
			if (coCoolDown != null)
			{
				StopCoroutine(coCoolDown);
				coCoolDown = null;
			}
		}

		text.CrossFadeAlpha(1f, 0f, false);
		if (isHidden)
		{
			isHidden = false;
			ChatUI.Instance.ReportEntryState(false);
		}
	}

	public void OnChatUnfocused()
	{
		if (isCoolingDown)
		{
			coCoolDown = StartCoroutine(CoolDown());
		}
		else
		{
			text.CrossFadeAlpha(0f, 0f, false);
			if (!isHidden)
			{
				isHidden = true;
				ChatUI.Instance.ReportEntryState(true);
			}
		}
	}

	IEnumerator CoolDown()
	{
		Vector2 sizeDelta = rect.sizeDelta;
		sizeDelta.x = 472f;
		rect.sizeDelta = sizeDelta;
		yield return WaitFor.Seconds(12f);
		if (!ChatUI.Instance.chatInputWindow.gameObject.activeInHierarchy)
		{
			text.CrossFadeAlpha(0.01f, 3f, false);
			if (!isHidden)
			{
				isHidden = true;
				ChatUI.Instance.ReportEntryState(true, true);
			}
		}
		else
		{
			yield break;
		}

		yield return WaitFor.Seconds(3f);
		isCoolingDown = false;
	}

	//state = is mouse button down on the scroll bar handle
	private void OnScrollInteract(bool state)
	{
		if (isHidden && state)
		{
			text.CrossFadeAlpha(1f, 0f, false);
			isHidden = false;
			ChatUI.Instance.ReportEntryState(false);
		}

		if (!isHidden && !state
		              && !ChatUI.Instance.chatInputWindow.gameObject.activeInHierarchy)
		{
			if (isCoolingDown)
			{
				if (coCoolDown != null)
				{
					StopCoroutine(coCoolDown);
					coCoolDown = null;
				}
			}

			isCoolingDown = true;
			coCoolDown = StartCoroutine(CoolDown());
		}
	}
}