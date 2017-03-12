using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Crafting {

    public class CraftingManager: MonoBehaviour {

        public CraftingDatabase Meals;

        private static CraftingManager craftingManager;

        public static CraftingManager Instance {
            get {
                if(!craftingManager) {
                    craftingManager = FindObjectOfType<CraftingManager>();
                }

                return craftingManager;
            }
        }
    }
}
