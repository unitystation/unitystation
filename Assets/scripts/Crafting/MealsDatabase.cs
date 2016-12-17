using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Ingredient {
    //string ingredientName;
    //int amount;
}

[System.Serializable]
public class Recipe {
    List<Ingredient> ingredients;
    //GameObject output;
}

public class MealsDatabase2 {

    private Dictionary<string, Recipe> meals = new Dictionary<string, Recipe>();

    private static MealsDatabase2 database;

    public static MealsDatabase2 instance {
        get {
            if(database == null) {
                database = new MealsDatabase2();
            }
            return database;
        }
    }

    public static Recipe TryGetMeal(string mealName) {
        Recipe recipe;
        instance.meals.TryGetValue(mealName, out recipe);
        return recipe;
    }
}

public class MealsDatabase: MonoBehaviour {

    public List<Recipe> recipes;

    void Start() {

    }
}
