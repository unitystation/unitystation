using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour {

    private static CraftingManager manager;

    public static CraftingManager instance {
        get {
            if(!manager) {
                manager = FindObjectOfType<CraftingManager>();
            }

            return manager;
        }
    }

    public MealsDatabase meals = new MealsDatabase();



}
