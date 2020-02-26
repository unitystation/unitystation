using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ChatBubble : MonoBehaviour
{
	[SerializeField] private Transform target;
	private Camera cam;

	[SerializeField]
	[Tooltip("The default font used when speaking.")]
	private TMP_FontAsset fontDefault;
	[SerializeField]
	private TextMeshProUGUI bubbleText;

	class BubbleMsg
	{
		public float maxTime; public string msg; public float elapsedTime = 0f;
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

	private void OnEnable()
	{
		cam = Camera.main;
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	/// <summary>
	/// Set and enable this ChatBubble
	/// </summary>
	public void SetupBubble(string message, float sizeMultiplier, float maxTime = 15f, TMP_FontAsset font = null)
	{
		if (font == null)
		{
			font = fontDefault;
		}


	}

    void UpdateMe()
    {
	    if (target != null)
	    {
		    Vector3 viewPos = cam.WorldToScreenPoint(target.position);
		    transform.position = viewPos;
	    }
    }
}
