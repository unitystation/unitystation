using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialStorage
{
	public Dictionary<string,int> Stored;
	public int MaximumStorage;
	public int Storageint;

	public bool AttemptConstruction (Dictionary<string,int> MaterialToRemove, int Quantity)
	{
		bool EnoughMaterials = true;
		foreach (KeyValuePair<string, int> Material in MaterialToRemove) 
		{
			if (Stored.ContainsKey (Material.Key)) {
				if ((Material.Value * Quantity) > Stored [Material.Key]) {
					EnoughMaterials = false;
				}
			} else {
				return (false);
			}
		}
		if (EnoughMaterials) {
			foreach (KeyValuePair<string, int> Material in MaterialToRemove) {
				Stored[Material.Key] = Stored[Material.Key] - (Material.Value * Quantity);
			}
			return (true);
		} else {
			return (false);
		}
	}
	public int AddMaterial (string Material, int Quantity)
	{
		Storageint = 0;
		foreach (KeyValuePair<string, int> Materials in Stored) {
			Storageint += Materials.Value;
		}
		int AvailableSpace = MaximumStorage - Storageint;
		if (AvailableSpace < Quantity) {
			Quantity = AvailableSpace;
		}
		if (Stored.ContainsKey (Material)) {
			Stored[Material] = Stored[Material] + Quantity;
		} else {
			Stored[Material] = Quantity;
		}
		return(Quantity);
	}
}