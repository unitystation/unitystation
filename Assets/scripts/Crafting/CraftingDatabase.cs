using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Crafting {
    
    [System.Serializable]
    public class CraftingDatabase {
        public Recipe[] recipeList;

        public GameObject FindRecipe(List<Ingredient> ingredients) {
            foreach(var recipe in recipeList) {
                if(recipe.Check(ingredients)) {
                    return recipe.output;
                }
            }
            return null;
        }
    }
}
