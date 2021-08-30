using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpriteStretch : MonoBehaviour
{
	public bool KeepAspectRatio;

	private Image image;

	void Start()
	{
		image = gameObject.GetComponent<Image>();
	}

	void Update()
	{
		var topRightCorner = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, Camera.main.transform.position.z));
		var worldSpaceWidth = topRightCorner.x * 2;
		var worldSpaceHeight = topRightCorner.y * 2;

		var spriteSize = image.sprite.bounds.size;

		var scaleFactorX = worldSpaceWidth / spriteSize.x;
		var scaleFactorY = worldSpaceHeight / spriteSize.y;

		if (KeepAspectRatio)
		{
			if (scaleFactorX > scaleFactorY)
			{
				scaleFactorY = scaleFactorX;
			}
			else
			{
				scaleFactorX = scaleFactorY;
			}
		}

		gameObject.transform.localScale = new Vector3(scaleFactorX, scaleFactorY, 1);
	}
}