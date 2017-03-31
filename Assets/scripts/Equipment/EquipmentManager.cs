using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Equipment
{
	public class EquipmentManager : MonoBehaviour
	{
		private static EquipmentManager equipmentManager;

		public static EquipmentManager Instance {
			get { 
				if (!equipmentManager) {
					equipmentManager = FindObjectOfType<EquipmentManager>();
				}
				return equipmentManager;
			}
		}
	}

	//Helps identify the position in syncEquip list
	public enum Epos
	{
		suit,
		belt,
		head,
		feet,
		face,
		mask,
		underwear,
		uniform,
		leftHand,
		rightHand,
		body,
		eyes,
		back,
		hands,
		ear,
		neck
	}
}
