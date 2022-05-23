using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_HornHonk : MonoBehaviour
{
	public void Hornhonk()
	{
		_ = SoundManager.Play(CommonSounds.Instance.ClownHonk);
	}
}
