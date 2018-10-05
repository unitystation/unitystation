using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChatEntry : MonoBehaviour {

    public Text text;
    private bool isCoolingDown = true;
	public RectTransform rect;

	private Coroutine coCoolDown;

    void OnEnable()
    {
        EventManager.AddHandler(EVENT.ChatFocused, OnChatFocused);
        EventManager.AddHandler(EVENT.ChatUnfocused, OnChatUnfocused);
        if (!ControlChat.Instance.chatInputWindow.gameObject.activeInHierarchy){
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

    public void OnChatFocused()
    {
        if (isCoolingDown)
        {
			if (coCoolDown != null) {
				StopCoroutine(coCoolDown);
				coCoolDown = null;
			}
        }
        text.CrossFadeAlpha(1f, 0f, false);
    }

    public void OnChatUnfocused()
    {
        if (isCoolingDown)
        {
			coCoolDown = StartCoroutine(CoolDown());
        } else
        {
            text.CrossFadeAlpha(0f, 0f, false);
        }
    }

    IEnumerator CoolDown()
    {
		Vector2 sizeDelta = rect.sizeDelta;
		sizeDelta.x = 472f;
		rect.sizeDelta = sizeDelta;
        yield return new WaitForSeconds(12f);
        if (!ControlChat.Instance.chatInputWindow.gameObject.activeInHierarchy)
        {
            text.CrossFadeAlpha(0.01f, 3f, false);
        } else
        {
            yield break;
        }
        yield return new WaitForSeconds(3f);
        isCoolingDown = false;
    }
}
