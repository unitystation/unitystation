using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Crafting
{

    [System.Serializable]
    public class Recipe
    {
        public string name;
        public Ingredient[] ingredients;
        public GameObject output;

        public bool Check(List<Ingredient> other)
        {
            foreach (var ingredient in ingredients)
            {
                if (!other.Contains(ingredient))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
