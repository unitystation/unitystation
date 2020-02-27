using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UI_OxygenAlert : TooltipMonoBehaviour
{

	private const float SpriteCycleTime = 1f; // cycle every 1 second
	public Sprite[] statusImages; //images to cycle between when active
	private int nextImageIndex = 1;

	public Image img;
	private float timeWait;

	public override string Tooltip => "Choking (No O2)";

	void Start()
	{
		img = GetComponent<Image>();
	}

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		ResetImg();
	}

	void UpdateMe()
	{
		timeWait += Time.deltaTime;
		if (timeWait > SpriteCycleTime)
		{
			CycleImg();
			timeWait -= SpriteCycleTime;
		}
	}

	void CycleImg()
	{
		img.sprite = statusImages.Wrap( nextImageIndex++ );
	}

	void ResetImg() {
		img.sprite = statusImages[0];
		nextImageIndex = 1;
		timeWait = 0f;
	}
}
