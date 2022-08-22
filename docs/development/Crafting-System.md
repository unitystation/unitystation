# Crafting System

This page will help you to create/remove recipes/categories. Any detailed information about the system available in the system's documentation (in the code) or Unity Editor's tooltips. But you can ask any question about the system in Discord - just ping Lizurt#4269.

## How to...

### How to create a new crafting recipe

1. Open the Unity Editor 
2. Follow the path: /Assets/ScriptableObjects/Crafting/Recipes/
3. Find a necessarry category to put a new recipe into.
4. RMB -> Create -> ScriptableObjects -> Crafting -> CraftingRecipe
5. Give the new recipe a name. 
6. Press the "Add to the singleton if necessary" button.
7. Edit your new recipe freely(tooltips will help you).
8. Add the opportunity for a player to learn this recipe.
   * You can add the recipe to the default known recipes list in the PlayerCrafting component of the Player_V4 prefab.
   * You can add the recipe to a recipe book.

### How to delete a crafting recipe

1. Open the Unity Editor 
2. Follow the path: /Assets/ScriptableObjects/Crafting/Recipes/
3. Find a necessarry crafting recipe.
4. Open the recipe.
5. Make sure to remove this recipe from any ways to learn this recipe.
   * You can skip this step - unit tests will help you to find missing asset references.
   * You can check Player's PlayerCrafting component.
   * You can check recipe books.
6. Press the "Prepare for deletion" button.
7. Now you can safely delete this recipe.
   * For example, RMB on the crafting recipe -> Delete

### How to create a new crafting category

1. Open /UnityProject/Assets/Scripts/Systems/CraftingV2/RecipeCategory.cs
2. Add a new category enum.
3. Open the Unity Editor.
4. Open the path: /Assets/ScriptableObjects/Crafting/Categories/
5. RMB -> Create -> ScriptableObjects -> Crafting -> CategoryAndIcon
6. Choose RecipeCategory (the enum that was added at step #2).
7. Choose CategoryIcon (usually they're located in /Assets/Recources/UI/CraftingCategoryIcons/).
8. Follow the path: /Assets/Prefabs/UI/Tabs/Recources/Crafting/CategoryButtons/
9. Press RMB on the _BaseCategoryButton -> Create -> Prefab variant.
10. Choose CategoryAndIcon that was created at the step #5.
11. Follow the path: /Assets/Prefabs/UI/Tabs/Recources/Crafting/CraftingMenu.prefab
12. Add your new category button to the CategoryButtonsPrefabs list (in the CraftingMenu script).

### How to delete a crafting category

1. Open the Unity Editor.
2. Make sure that there are no recipes that use this recipe category.
   * Usually these recipes are located in the /Assets/ScriptableObjects/Crafting/Recipes/*yourcategory*
3. Follow the path: /Assets/Prefabs/UI/Tabs/Recources/Crafting/CraftingMenu.prefab
4. Remove the category from CategoryButtonsPrefabs list (in the CraftingMenu script).
5. Follow the path: /Assets/Prefabs/UI/Tabs/Recources/Crafting/CategoryButtons/
6. Find a necessary category button (look at the CategoryAndIcon field in the CategoryButtonScript component).
7. Delete the category button.
8. Follow the path: /Assets/ScriptableObjects/Crafting/Categories/
9. Find a necessary CategoryAndIcon (look at the RecipeCategory field).
10. Delete the necessary CategoryAndIcon.
11. Follow the path: /Assets/Scripts/Systems/CraftingV2/RecipeCategory.cs
12. Remove the necessary recipe category enum value.

### How to create a new recipe book.

1. Open the Unity Editor.
2. Follow the path: /Assets/Prefabs/Items/Bureaucracy/RecipeBook/
3. RMB on the _Base_RecipeBook -> Create -> Prefab variant.
4. Add recipes to the ContainsRecipes list of the RecipeBook component.
5. Optionally, add remarks, edit book's settings.
6. Optionally, add the book on a map.

## FAQ

### The singleton has nulls or some recipes in the singleton have a wrong index. What should I do?

Don't worry, you don't have to manually fix the singleton or a recipes' index.

1. Open the Unity Editor.
2. Open the CraftingRecipeSingleton.
3. Press the button "Remove nulls and fix recipes' indexes" below.

### Reagents don't work as ingredients. What should I do?

Perhaps the ChemistryReagentsSO singleton has reagents with a wrong index. You also probably can notice an error in the console that says something about it.

1. Open the Unity Editor.
2. Open the ChemistryReagentsSO singleton.
3. Press the button "Fix reagents' indexes" below.
