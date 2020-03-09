using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
public class SetVolume : MonoBehaviour
{

		public void SetLevel(float sliderValue)
		{
			AudioListener.volume = sliderValue;
		}
}
