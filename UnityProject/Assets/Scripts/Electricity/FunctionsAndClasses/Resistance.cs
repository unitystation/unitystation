using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class Resistance { //a cheeky way to get pointers instead of copies without pinning anything 
	public float Ohms = 0; 
	public bool ResistanceAvailable = true; // if false this resistance is not calculated
}
