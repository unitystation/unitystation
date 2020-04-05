﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdminTools
{
	public class AdminGlobalSoundSearchText : MonoBehaviour
	{
		public void OnSearch() //called when search field is changed or finished being edited
		{
			gameObject.transform.parent.GetComponent<AdminGlobalSoundSearchBar>().Search();
		}
	}
}
