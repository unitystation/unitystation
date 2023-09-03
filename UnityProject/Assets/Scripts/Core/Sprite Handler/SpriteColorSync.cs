using System;
using Logs;
using Mirror;
using UnityEngine;

/// <summary>
/// Syncs color/alpha for provided spriterenderer
/// </summary>
public class SpriteColorSync : NetworkBehaviour
{
	[SerializeField] private SpriteRenderer spriteToColor = default;

	[SyncVar(hook = nameof(ColorChanged))]
	private Color Color;

	public ColorChangedEvent OnColorChange = new ColorChangedEvent();
	public SpriteRenderer SpriteRenderer => spriteToColor;

	/// <summary>
	/// For smooth transition over time
	/// </summary>
	[SyncVar(hook = nameof(TimeChanged))]
	private float Time = 0;

	//goes from 0 to 1
	private float lerpProgress = 0;


	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	private void UpdateMe()
	{
		if (Time > 0 && spriteToColor && spriteToColor.color != Color)
		{
			spriteToColor.color = Color.Lerp(spriteToColor.color, Color, lerpProgress);
			lerpProgress += UnityEngine.Time.deltaTime/Time;
			OnColorChange.Invoke(spriteToColor.color);
		}
	}

	[Server]
	public void SetColorServer(Color newColor)
	{
		if (Color == newColor)
		{
			return;
		}
		Loggy.LogFormat("Color changed to {0}", Category.Sprites, newColor.ToString());
		ColorChanged(newColor,newColor);
	}

	[Server]
	public void SetTransitionTime(float time)
	{
		TimeChanged(time, time);
	}

	private void ColorChanged(Color oldColor, Color newColor)
	{
		Color = newColor;
		ApplyColor();
	}
	private void TimeChanged(float oldTime, float newTime)
	{
		Time = newTime;
	}

	private void ApplyColor()
	{
		if (Time <= 0 && spriteToColor != null)
		{
			spriteToColor.color = Color;
			OnColorChange.Invoke(spriteToColor.color);
		}

		lerpProgress = 0;
	}
}
