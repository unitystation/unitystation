using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
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
		ins = this;
		//Make sure to add your sprite on load
		SpriteDictionary ["hand"] = Resources.Load<Sprite> ("UI/RightClickButtonIcon/" + "hand");
		SpriteDictionary ["Magnifying_glass"] = Resources.Load<Sprite> ("UI/RightClickButtonIcon/" + "Magnifying_glass");
		SpriteDictionary ["question_mark"] = Resources.Load<Sprite> ("UI/RightClickButtonIcon/" + "question_mark");
		SpriteDictionary ["Drag_Hand"] = Resources.Load<Sprite> ("UI/RightClickButtonIcon/" + "Drag_Hand");
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
			ItemAttributes Nameues = objects[i].GetComponent<ItemAttributes>();
			if (Nameues == null) {
				newMenu.title = "Unknown";
			} else {
				newMenu.title = Nameues.itemName;
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
					var attributes = method.GetCustomAttributes(typeof(contextMethod), true);
					if (attributes.Length > 0) {
						
						//Logger.Log("Script: " + mono + " Method: " + method.ToString(), Category.UI);
						//Logger.Log (method.ToString (), Category.UI);
						Menu NewSubMenu = new Menu();
						contextMethod contextMethodMenu = (contextMethod)method.GetCustomAttributes (typeof(contextMethod), true)[0];
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
			ins.options.Add (newMenu);
		}

	}
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class contextMethod : Attribute
{
	public string ButtonTitle;
	public string SpriteName;

	public contextMethod(string ButtonTitle,string SpriteName)
	{
		this.ButtonTitle = ButtonTitle;
		this.SpriteName = SpriteName;
	}
}

