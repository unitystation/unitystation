using System;
using System.Text;
using System.IO;
using UnityEngine;

public class GoogleCloudTTS : MonoBehaviour {

	public static GoogleCloudTTS Instance;
	private const string apiKey = "AIzaSyBqb3wPpfnlCWyARnRVSClBkLBSYXr05_Q";

	private void Awake()
	{
		if(Instance == null){
			Instance = this;
		} //else Gets destroyed by parent
	}

	private void SynthesizeSuccess(string audioData){
		
	}

	public AudioClip GetClipFromBase64(string Base64data){
		AudioClip newClip = null;
		byte[] data = Convert.FromBase64String(Base64data);
		newClip.SetData(ConvertByteToFloat(data), 44);
		return newClip;
	}

	private static float[] ConvertByteToFloat(byte[] array)
	{
		float[] floatArr = new float[array.Length / 2];

		for (int i = 0; i < floatArr.Length; i++) {
			floatArr[i] = ((float)BitConverter.ToInt16(array, i * 2)) / 32768.0f;
		}

		return floatArr;
	}
}
