using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Logs;
using TMPro;
using UnityEngine;

public class ChatBubble : MonoBehaviour, IDisposable
{

	public Transform content;
	public Transform Pool;


	[SerializeField] private Transform target;
	public Transform Target => target;
	private Camera cam;

	[SerializeField] [Tooltip("The default font used when speaking.")]
	private TMP_FontAsset fontDefault = null;

	[SerializeField] [Tooltip("The font used when the player is an abominable clown.")]
	private TMP_FontAsset fontClown = null;


	[SerializeField]
	[Tooltip(
		"The maximum length of text inside a single text bubble. Longer texts will display multiple bubbles sequentially.")]
	[Range(1, 200)]
	private int maxMessageLength = 64;

	public class BubbleMsg
	{
		public float maxTime;
		public string msg;
		public float elapsedTime = 0f;

		//Character showing delay vars
		internal int characterIndex = 0;

		//Use invis characters so text box doesnt resize
		//keeps characters in the same place as characters appear (easier to read)
		public bool buffered = true;

		//Text appears instantly instead of pop in, from player prefs
		public bool instantText = false;

		//Character pop in speed, from player prefs
		//TODO maybe add override?
		internal float characterPopInSpeed;

		internal ChatModifier modifier;
	}

	private Queue<BubbleMsg> msgQueue = new Queue<BubbleMsg>();
	private bool showingDialogue = false;

	/// <summary>
	/// A cache for the cache bubble rect transform. For performance!
	/// </summary>
	private RectTransform chatBubbleRectTransform;

	/// <summary>
	/// Minimum time a bubble's text will be visible on-screen.
	/// </summary>
	private const float displayTimeMin = 1.8f;

	/// <summary>
	/// Maximum time a bubble's text will be visible on-screen.
	/// </summary>
	private const float displayTimeMax = 15f;

	/// <summary>
	/// Seconds required to display a single character.
	/// Approximately 102 WPM / 510 CPM with a value of 0.12f
	/// </summary>
	private const float displayTimePerCharacter = 0.12f;

	/// <summary>
	/// When calculating the time it will take to display all chat bubbles in the queue,
	/// each bubble is considered to have this many additional characters.
	/// This causes the queue to be displayed more quickly based on its length.
	/// </summary>
	private const float displayTimeCharactersPerBubble = 10f;

	/// <summary>
	/// Multiplies the elapse of display time per character left in the msgQueue (after the first element).
	/// </summary>
	private static float displayTimeMultiplierPerSecond = 0.09f;



	private CancellationTokenSource cancelSource;


	public SubChatBubble BubblePrefab;

	public List<SubChatBubble> ActiveBubbles = new List<SubChatBubble>();
	public List<SubChatBubble> PooledBubbles = new List<SubChatBubble>();

	/// <summary>
	/// Calculates the time required to display the message queue.
	/// The first message is excluded.
	/// Each bubble is also counted how have a bunch of "fake" characters (displayTimeCharactersPerBubble)
	/// to reduce the delay between each bubble.
	/// </summary>
	/// <param name="queue">The current message queue. The first element will be excluded!</param>
	/// <returns>Time required to display the queue (without the first element).</returns>
	private float TimeToShow(Queue<BubbleMsg> queue)
	{
		float total = 0;
		foreach (BubbleMsg msg in queue.Skip(1))
		{
			total += msg.maxTime;
			total += displayTimePerCharacter * displayTimeCharactersPerBubble;
		}

		return total;
	}

	/// <summary>
	/// Used to calculate showing length time
	/// </summary>
	private float TimeToShow(int charCount)
	{
		return Mathf.Clamp((float) charCount * displayTimePerCharacter, displayTimeMin, displayTimeMax);
	}

	private void OnEnable()
	{
		cam = Camera.main;
		UpdateManager.Add(CallbackType.POST_CAMERA_UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.POST_CAMERA_UPDATE, UpdateMe);
	}

	public void AppendToBubble(string newMessage, ChatModifier chatModifier = ChatModifier.None)
	{
		QueueMessages(newMessage, chatModifier);
	}

	/// <summary>
	/// Set and enable this ChatBubble
	/// </summary>
	public void SetupBubble(Transform _target, string msg, ChatModifier chatModifier = ChatModifier.None)
	{
		if (cam == null) cam = Camera.main;

		Vector3 viewPos = cam.WorldToScreenPoint(_target.position);
		transform.position = viewPos;

		gameObject.SetActive(true);
		target = _target;
		QueueMessages(msg, chatModifier);

		cancelSource = new CancellationTokenSource();
		if (showingDialogue == false)
		{
			showingDialogue = true;
			StartCoroutine(ShowDialogue(cancelSource.Token));
		}
	}

	private void QueueMessages(string msg, ChatModifier chatModifier = ChatModifier.None)
	{
		if (msg.Length > maxMessageLength)
		{
			while (msg.Length > maxMessageLength)
			{
				int ws = -1;
				//Searching for the nearest whitespace
				for (int i = maxMessageLength; i >= 0; i--)
				{
					if (char.IsWhiteSpace(msg[i]))
					{
						ws = i;
						break;
					}
				}

				//Player is spamming with no whitespace. Cut it up
				if (ws == -1 || ws == 0) ws = maxMessageLength + 2;

				var split = msg.Substring(0, ws);
				msgQueue.Enqueue(new BubbleMsg
					{maxTime = TimeToShow(split.Length), msg = split, modifier = chatModifier});

				msg = msg.Substring(ws + 1);
				if (msg.Length <= maxMessageLength)
				{
					msgQueue.Enqueue(new BubbleMsg
						{maxTime = TimeToShow(msg.Length), msg = msg, modifier = chatModifier});
				}
			}
		}
		else
		{
			msgQueue.Enqueue(new BubbleMsg {maxTime = TimeToShow(msg.Length), msg = msg, modifier = chatModifier});
		}

	}



	private IEnumerator ShowDialogue(CancellationToken cancelToken)
	{
		showingDialogue = true;

		if (msgQueue.Count == 0)
		{
			yield return WaitFor.EndOfFrame;
			yield break;
		}

		while (msgQueue.Count > 0 || ActiveBubbles.Count > 0)
		{
			while (msgQueue.Count == 0 && ActiveBubbles.Count > 0)
			{
				yield return null;
			}

			var NumberMaxShow = PlayerPrefs.GetInt(PlayerPrefKeys.ChatBubbleNumber, 2);
			while (ActiveBubbles.Count >= NumberMaxShow)
			{
				yield return null;
			}



			while (ActiveBubbles.Count > 0 && ActiveBubbles[^1].FinishedDisplaying == false)
			{
				yield return null;
			}

			if (msgQueue.Count > 0)
			{
				BubbleMsg msg = msgQueue.Dequeue();

				var Bubble = GetSubChatBubble();
				Bubble.transform.SetParent(content);
				Bubble.transform.SetSiblingIndex(Bubble.transform.parent.childCount -1);
				Bubble.DoShowDialogue(cancelToken, msg);
				ActiveBubbles.Add(Bubble);
			}
		}

		showingDialogue = false;
		ReturnToPool();
	}


	public SubChatBubble GetSubChatBubble()
	{
		if (PooledBubbles.Count > 0)
		{
			var bubble = PooledBubbles[0];
			PooledBubbles.RemoveAt(0);
			bubble.gameObject.SetActive(true);
			return bubble;
		}

		var bubble2 = Instantiate(BubblePrefab, this.gameObject.transform);
		bubble2.ChatBubble = this;
		return bubble2;
	}

	void UpdateMe()
    {
	    if (target != null)
	    {
		    Vector3 viewPos = manualWorldToScreenPoint(target.position);
		    transform.position = viewPos;
	    }
    }

	Vector3 manualWorldToScreenPoint(Vector3 wp) {
		// calculate view-projection matrix

		var worldToCameraMatrix  = Matrix4x4.Inverse(Matrix4x4.TRS(
			cam.transform.position,
			cam.transform.rotation,
			new Vector3(1, 1, -1)
		) );

		Matrix4x4 mat = cam.projectionMatrix * worldToCameraMatrix;

		// multiply world point by VP matrix
		Vector4 temp = mat * new Vector4(wp.x, wp.y, wp.z, 1f);

		if (temp.w == 0f) {
			// point is exactly on camera focus point, screen point is undefined
			// unity handles this by returning 0,0,0
			return Vector3.zero;
		} else {
			// convert x and y from clip space to window coordinates
			temp.x = (temp.x/temp.w + 1f)*.5f * cam.pixelWidth;
			temp.y = (temp.y/temp.w + 1f)*.5f * cam.pixelHeight;
			return new Vector3(temp.x, temp.y, wp.z);
		}
	}

    public void ReturnToPool()
    {
	    if(cancelSource != null) cancelSource.Cancel();

	    //bubbleText.text = "";
	    //chatBubble.SetActive(false);
	    gameObject.SetActive(false);
	    showingDialogue = false;
	    target = null;
    }

    public void Dispose()
    {
	    cancelSource?.Dispose();
    }

    public static void SetPreferenceNummberBubbles(int NummberBubbles)
    {
	    PlayerPrefs.SetInt(PlayerPrefKeys.ChatBubbleNumber, NummberBubbles);
    }

    public static int GetPreferenceNummberBubbles()
    {
	    return PlayerPrefs.GetInt(PlayerPrefKeys.ChatBubbleNumber, 3);
    }
}
