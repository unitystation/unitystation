using System;
using UnityEngine;
using UnityEngine.UI;

public class ClickyButton : MonoBehaviour
{


	public void Start()
	{

		this.GetComponent<ToggleButton>()?.onValueChanged?.AddListener(Click);
		this.GetComponent<Button>()?.onClick?.AddListener(Click);

	}

	public void Click()
	{
		Click(true);
	}

	public void Click(bool val)
	{
		_ = SoundManager.Play(CommonSounds.Instance.Click01);
	}
}
