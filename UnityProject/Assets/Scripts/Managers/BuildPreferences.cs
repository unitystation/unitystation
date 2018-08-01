using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class BuildPreferences : MonoBehaviour {

	public static bool isForRelease { get{
			CheckPlayerPrefs();
			return PlayerPrefs.GetInt("ReleaseMode") == 1;
		}}
	
	public static void SetRelease(bool isOn){
		CheckPlayerPrefs();
		if(isOn){
			PlayerPrefs.SetInt("ReleaseMode", 1);
		} else {
			PlayerPrefs.SetInt("ReleaseMode", 0);
		}
		PlayerPrefs.Save();
	}

	static void CheckPlayerPrefs(){
		if (!PlayerPrefs.HasKey("ReleaseMode")) {
			PlayerPrefs.SetInt("ReleaseMode", 0);
			PlayerPrefs.Save();
		}
	}
}
