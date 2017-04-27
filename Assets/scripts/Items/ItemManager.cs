﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PlayGroup;
using UI;

namespace Items {

	public class ItemManager: MonoBehaviour {
		private static ItemManager itemManager;
		public static ItemManager Instance {
			get {
				if(!itemManager) {
					itemManager = FindObjectOfType<ItemManager>();
				}
				return itemManager;
			}
		}
	}
}