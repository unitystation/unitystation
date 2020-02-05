using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChatEntry : MonoBehaviour
{
	[SerializeField] private Text normalText;
	[SerializeField] private Text adminText;
	[SerializeField] private GameObject adminOverlay;
	[SerializeField] private Shadow shadow;

	/// <summary>
	/// The current message of the ChatEntry
	/// </summary>
	public string Message => normalText.text;

	private bool isCoolingDown = true;
	public RectTransform rect;

	private Coroutine coCoolDown;
	private bool isHidden = false;
	public GameObject stackTimesObj;
	public Text stackTimesText;
	private Image stackCircle;
	private int stackTimes = 1;
	private string adminId;
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

	public void SetText(string msg)
	{
		normalText.text = msg;
		adminText.text = msg;
	}

	public void SetAdminPrivateMsg(string msg, string adminID)
	{
		adminId = adminID;
		SetText(msg);
		normalText.text += "\r\n filler \r\n filler";
		adminOverlay.SetActive(true);
		shadow.enabled = false;
		adminText.gameObject.SetActive(true);
	}

	public void ReplyToAdminMessage()
	{
		ChatUI.Instance.OpenAdminReply(adminText.text, adminId);
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

		SetCrossFadeAlpha(1f, 0f);
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
			if(coCoolDown != null) StopCoroutine(coCoolDown);

			coCoolDown = StartCoroutine(CoolDown());
		}
		else
		{
			SetCrossFadeAlpha(0f, 0f);
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
		yield return WaitFor.EndOfFrame;
		SetStackPos();
		yield return WaitFor.Seconds(12f);
		if (!ChatUI.Instance.chatInputWindow.gameObject.activeInHierarchy)
		{
			SetCrossFadeAlpha(0.01f, 3f);
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

	public void AddChatDuplication()
	{
		stackTimes++;
		stackTimesText.text = $"x{stackTimes}";
		stackTimesObj.SetActive(true);
		StartCoroutine(StackPumpAnim());
		SetCrossFadeAlpha(1f, 0f);
		if (isCoolingDown)
		{
			if(coCoolDown != null) StopCoroutine(coCoolDown);
			coCoolDown = StartCoroutine(CoolDown());
		}
		else
		{
			isCoolingDown = true;
			coCoolDown = StartCoroutine(CoolDown());
		}
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

	private bool stackPosSet = false;

	void SetStackPos()
	{
		if (string.IsNullOrEmpty(normalText.text)) return;
		
		if (stackPosSet) return;
		stackPosSet = true;

		string _text = normalText.text;

		TextGenerator textGen = new TextGenerator(_text.Length);
		Vector2 extents = normalText.gameObject.GetComponent<RectTransform>().rect.size;
		textGen.Populate(_text, normalText.GetGenerationSettings(extents));
		if (textGen.vertexCount == 0)
		{
			return;
		}

		var newPos = stackTimesObj.transform.localPosition;
		newPos.x = (textGen.verts[textGen.vertexCount - 1].position / normalText.canvas.scaleFactor).x;


		if (rect.rect.height < 30f)
		{
			newPos.y += 3.5f * rect.localScale.y;
		}

		stackTimesObj.transform.localPosition = newPos;
	}

	void SetCrossFadeAlpha(float amt, float time)
	{
		normalText.CrossFadeAlpha(amt, time, false);
		stackTimesText.CrossFadeAlpha(amt, time, false);
		stackCircle.CrossFadeAlpha(amt, time, false);
	}
}