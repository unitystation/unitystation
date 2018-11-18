using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class Rightclick : MonoBehaviour {
	public static Rightclick ins;
	public bool Initialise = false;
	public List<Menu> options = new List<Menu>();

	public List<Sprite> Spritenames = new List<Sprite>(){

	};
	public class ButtonEntry
	{
		public string Title;
	}
	public Dictionary<string,int> MenuOrder = new Dictionary<string,int>(){
		["Examine"] = 1, 
		["Pick Up"] = 2,
		["Drag"] = 3, 
		["Open/close"] = 4, 
		["Details"] = 5,
		["Turn on/Turn off"] = 6, 
		["Toggle Charge"] = 7, 
		["Toggle Support"] = 8, 
		["Unknown"] = 9, 

	};

	public Dictionary<string, Sprite> SpriteDictionary = new Dictionary<string, Sprite>()	{	};

	public class Menu {
		public Color colour;
		public Sprite sprite;
		public string title;
		public GameObject Item;
		public List<Menu> SubMenus = new List<Menu>();
		public MethodInfo Method;
		public MonoBehaviour Mono;
	}
		

	void Awake(){
		if(ins == null){
			ins = this;
		} else {
			Destroy(this);
		}
	
		//Make sure to add your sprite on load
		SpriteDictionary ["hand"] = Resources.Load<Sprite> ("UI/RightClickButtonIcon/" + "hand");
		SpriteDictionary ["Magnifying_glass"] = Resources.Load<Sprite> ("UI/RightClickButtonIcon/" + "Magnifying_glass");
		SpriteDictionary ["question_mark"] = Resources.Load<Sprite> ("UI/RightClickButtonIcon/" + "question_mark");
		SpriteDictionary ["Drag_Hand"] = Resources.Load<Sprite> ("UI/RightClickButtonIcon/" + "Drag_Hand");
		SpriteDictionary ["Power_Button"] = Resources.Load<Sprite> ("UI/RightClickButtonIcon/" + "Power_Button");
	}


	void Update () {
		if (Input.GetMouseButtonDown (1)) {
			Vector3 position = Camera.main.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
			position.z = 0f;
			//gets Items on the position of the mouse
			List<GameObject> objects = UITileList.GetItemsAtPosition(position);
			//Generates menus
			Generate (objects);
			//Logger.Log ("yo", Category.UI);
			if (options.Count > 0){
				RadialMenuSpawner.ins.SpawnRadialMenu(options);
			}

		}
		
	}
	private void Generate(List<GameObject> objects){
		options = new List<Menu> ();
		for (int i = 0; i < objects.Count; i++) {
			Menu newMenu = new Menu();
			newMenu.colour = Color.gray;
			ItemAttributes ItemAttribute = objects[i].GetComponent<ItemAttributes>();
			if (ItemAttribute == null) {
				newMenu.title = "Unknown";
			} else {
				newMenu.title = ItemAttribute.itemName;
			}

			SpriteRenderer UseSprite = objects[i].GetComponentInChildren<SpriteRenderer>();
			if (UseSprite == null) {
				newMenu.sprite = ins.Spritenames [0];
			} else {
				newMenu.sprite = UseSprite.sprite;
			}

			//Find all monoBehaviours on object and store in a list
			MonoBehaviour[] scriptComponents = objects[i].GetComponents<MonoBehaviour>();

			//For each monoBehaviour in the list of script components
			foreach (MonoBehaviour mono in scriptComponents) {
				Type monoType = mono.GetType();
				foreach (MethodInfo method in monoType.GetMethods(BindingFlags.Public | BindingFlags.Instance)) {
					var attributes = method.GetCustomAttributes(typeof(ContextMethod), true);
					if (attributes.Length > 0) {
						
						//Logger.Log("Script: " + mono + " Method: " + method.ToString(), Category.UI);
						//Logger.Log (method.ToString (), Category.UI);
						Menu NewSubMenu = new Menu();
						ContextMethod contextMethodMenu = (ContextMethod)method.GetCustomAttributes (typeof(ContextMethod), true)[0];
						NewSubMenu.colour = Color.gray;
						NewSubMenu.Item = objects[i];			
						NewSubMenu.title = contextMethodMenu.ButtonTitle;
						NewSubMenu.sprite =  SpriteDictionary[contextMethodMenu.SpriteName];
						NewSubMenu.Mono = mono;
						NewSubMenu.Method = method;

						newMenu.SubMenus.Add (NewSubMenu);

					}
				}
			}

			//for (int L = 0; L < 3; L++) {
			//	Menu NewSubMenu = new Menu();
			//	NewSubMenu.colour = Color.gray;
			//	NewSubMenu.title = "sub " + ins.names[L];
			//	NewSubMenu.sprite = ins.Spritenames[0];
			//	newMenu.SubMenus.Add (NewSubMenu);
			//}
			//Sort
			List<Menu> Sortlist = newMenu.SubMenus;
			Menu[] array = new Menu[MenuOrder.Count+1];

			foreach (Menu SubMenu in Sortlist) {
				array[MenuOrder[SubMenu.title]] = SubMenu;
			}

			newMenu.SubMenus = new List<Menu>();

			for (int S = 0; S < array.Length; S++)  {
				if (!(array[S] == null)) {
					newMenu.SubMenus.Add (array [S]);
				}
			}

			ins.options.Add (newMenu);
		}
	}
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class ContextMethod : Attribute
{
	public string ButtonTitle;
	public string SpriteName;

	public ContextMethod(string ButtonTitle,string SpriteName)
	{
		this.ButtonTitle = ButtonTitle;
		this.SpriteName = SpriteName;
	}
}

