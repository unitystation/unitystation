using Mirror;
using UnityEngine;

/// <summary>
/// Syncs color/alpha for provided spriterenderer
/// </summary>
public class SpriteColorSync : NetworkBehaviour
{
	[SerializeField] private SpriteRenderer spriteToColor;

	[SyncVar(hook = nameof(ColorChanged))]
	private Color Color;

	[Server]
	public void SetColorServer(Color newColor)
	{
		ColorChanged(newColor,newColor);
	}

	private void ColorChanged(Color oldColor, Color newColor)
	{
		Color = newColor;
		ApplyColor();
	}

	private void ApplyColor()
	{
		if (spriteToColor != null)
		{
			spriteToColor.color = Color;
		}
	}
}