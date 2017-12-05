using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Crafting
{

    public class CraftingManager : MonoBehaviour
    {
        [SerializeField]
        private CraftingDatabase meals = new CraftingDatabase();
        public static CraftingDatabase Meals { get { return Instance.meals; } }

        private static CraftingManager craftingManager;

        public static CraftingManager Instance
        {
            get
            {
                if (!craftingManager)
                {
                    craftingManager = FindObjectOfType<CraftingManager>();
                }

                return craftingManager;
            }
        }
    }
}
