using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class Rightclick : MonoBehaviour
{
	public static Rightclick ins;
	public bool Initialise = false;
	public List<Menu> options = new List<Menu>();

	public List<Sprite> Spritenames = new List<Sprite>()
	{
	};

	public class ButtonEntry
	{
		public string Title;
	}

	public Dictionary<string, int> MenuOrder = new Dictionary<string, int>()
	{
		["Examine"] = 1,
		["Pick Up"] = 2,
		["Pull"] = 3,
		["Open/close"] = 4,
		["Details"] = 5, ["Turn on/Turn off"] = 6,
		["Toggle Charge"] = 7,
		["Toggle Support"] = 8,
		["Add to"] = 9,
		["Pour out"] = 10,
		["Contents"] = 11,
		["Unknown"] = 12,
	};

	public Dictionary<string, Func<bool>> CheckDictionary = new Dictionary<string, Func<bool>>()
	{
		//["Test"] = ElectricalSynchronisation.CheckIseLectricalMan
	};

	public Dictionary<string, Sprite> SpriteDictionary = new Dictionary<string, Sprite>() { };

	public Dictionary<InterColour, string> ColourDictionary = new Dictionary<InterColour, string>() { };
	/// saved reference to lighting sytem, for checking FOV occlusion
	private LightingSystem lightingSystem;


	public class Menu
	{
		public Color colour;
		public Sprite sprite;
		public string title;
		public Sprite BackgroundSprite;

		public GameObject Item;
		public List<Menu> SubMenus = new List<Menu>();
		public MethodInfo Method;
		public MonoBehaviour Mono;
	}

	private void Start()
	{
		lightingSystem = Camera.main.GetComponent<LightingSystem>();
	}

	void Awake()
	{
		if (ins == null)
		{
			ins = this;
		}
		else
		{
			Destroy(this);
		}

		//Make sure to add your sprite on load
		SpriteDictionary["hand"] = Resources.Load<Sprite>("UI/RightClickButtonIcon/" + "hand");
		SpriteDictionary["Magnifying_glass"] = Resources.Load<Sprite>("UI/RightClickButtonIcon/" + "Magnifying_glass");
		SpriteDictionary["question_mark"] = Resources.Load<Sprite>("UI/RightClickButtonIcon/" + "question_mark");
		SpriteDictionary["Drag_Hand"] = Resources.Load<Sprite>("UI/RightClickButtonIcon/" + "Drag_Hand");
		SpriteDictionary["Power_Button"] = Resources.Load<Sprite>("UI/RightClickButtonIcon/" + "Power_Button");
		SpriteDictionary["TestBG"] = Resources.Load<Sprite>("UI/RightClickButtonIcon/" + "TestBG");
		SpriteDictionary["Circle"] = Resources.Load<Sprite>("UI/RightClickButtonIcon/" + "Circle");

		SpriteDictionary["Science_flask"] = Resources.Load<Sprite>("UI/RightClickButtonIcon/" + "Science_flask");
		SpriteDictionary["Pour_into"] = Resources.Load<Sprite>("UI/RightClickButtonIcon/" + "Pour_into");
		SpriteDictionary["Pour_away"] = Resources.Load<Sprite>("UI/RightClickButtonIcon/" + "Pour_away");
		SpriteDictionary["Spill"] = Resources.Load<Sprite>("UI/RightClickButtonIcon/" + "Spill");

		ColourDictionary[InterColour.Default] = "#BEBEBE";
		ColourDictionary[InterColour.Test] = "#4cffeb";
	}


	void Update()
	{
		// Get right mouse click and check if mouse point occluded by FoV system.
		if (Input.GetMouseButtonDown(1) && lightingSystem.IsScreenPointVisible(Input.mousePosition))
		{
			//gets Items on the position of the mouse that are able to be right clicked
			List<GameObject> objects = GetRightClickableObjects();
			//Generates menus
			Generate(objects);
			//Logger.Log ("yo", Category.UI);
			if (options.Count > 0)
			{
				RadialMenuSpawner.ins.SpawnRadialMenu(options);
			}
		}
	}

	private List<GameObject> GetRightClickableObjects()
	{
		Vector3 position = Camera.main.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
		position.z = 0f;
		List<GameObject> objects = UITileList.GetItemsAtPosition(position);

		//special case, remove wallmounts that are transparent
		objects.RemoveAll(IsHiddenWallmount);

		return objects;
	}

	private bool IsHiddenWallmount(GameObject obj)
	{
		WallmountBehavior wallmountBehavior = obj.GetComponent<WallmountBehavior>();
		if (wallmountBehavior == null)
		{
			//not a wallmount
			return false;
		}

		return wallmountBehavior.IsHiddenFromLocalPlayer();
	}

	private void Generate(List<GameObject> objects)
	{
		options = new List<Menu>();
		for (int i = 0; i < objects.Count; i++)
		{
			Menu newMenu = new Menu();
			IRightClick Override = objects[i].GetComponent<IRightClick>();
			if (Override == null)
			{
				newMenu = new Menu();
			}
			else
			{
				//newMenu = Override.MenuOverride;
				newMenu.sprite = Override.MenuOverride.sprite;
				newMenu.colour = Override.MenuOverride.colour;
				newMenu.title = Override.MenuOverride.title;
			}

			if (newMenu.colour.a == 0)
			{
				newMenu.colour = Color.gray;
			}

			//newMenu.colour = Color.gray;
			if (newMenu.title == null)
			{
				string TitleName = objects[i].name;
				if (TitleName == null)
				{
					newMenu.title = "Unknown";
				}
				else
				{
					newMenu.title = TitleName;
				}
			}

			if (newMenu.sprite == null)
			{
				SpriteRenderer UseSprite = objects[i].GetComponentInChildren<SpriteRenderer>();
				if (UseSprite == null)
				{
					newMenu.sprite = ins.Spritenames[0];
				}
				else
				{
					newMenu.sprite = UseSprite.sprite;
				}
			}
			//ItemAttributes ItemAttribute = objects[i].GetComponent<ItemAttributes>();
			//if (ItemAttribute == null) {
			//newMenu.title = "Unknown";
			//} else {
			//newMenu.title = ItemAttribute.itemName;
			//}


			//Find all monoBehaviours on object and store in a list
			MonoBehaviour[] scriptComponents = objects[i].GetComponents<MonoBehaviour>();

			//For each monoBehaviour in the list of script components
			foreach (MonoBehaviour mono in scriptComponents)
			{
				Type monoType = mono.GetType();
				foreach (MethodInfo method in monoType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
				{
					var attributes = method.GetCustomAttributes(typeof(ContextMethod), true);
					if (attributes.Length > 0)
					{
						//Logger.Log("Script: " + mono + " Method: " + method.ToString(), Category.UI);
						//Logger.Log (method.ToString (), Category.UI);
						bool CanPass = true;
						ContextMethod contextMethodMenu =
							(ContextMethod) method.GetCustomAttributes(typeof(ContextMethod), true)[0];

						if (contextMethodMenu.ToCheck != null)
						{
							if (CheckDictionary.ContainsKey(contextMethodMenu.ToCheck))
							{
								if (!CheckDictionary[contextMethodMenu.ToCheck]())
								{
									CanPass = false;
								}
							}
						}

						if (CanPass)
						{
							Menu NewSubMenu = new Menu();
							if (contextMethodMenu.InterColour == InterColour.Null)
							{
								if (contextMethodMenu.colourHex == null)
								{
									NewSubMenu.colour = Color.gray;
								}
								else
								{
									ColorUtility.TryParseHtmlString(contextMethodMenu.colourHex, out NewSubMenu.colour);
									//NewSubMenu.colour = newCol;
								}
							}
							else
							{
								ColorUtility.TryParseHtmlString(ColourDictionary[contextMethodMenu.InterColour],
									out NewSubMenu.colour);
								//NewSubMenu.colour = newCol;
							}

							if (contextMethodMenu.BGSpriteName != null)
							{
								//Logger.Log ("Getting set", Category.UI);
								NewSubMenu.BackgroundSprite = SpriteDictionary[contextMethodMenu.BGSpriteName];
							}

							NewSubMenu.Item = objects[i];
							NewSubMenu.title = contextMethodMenu.ButtonTitle;
							NewSubMenu.sprite = SpriteDictionary[contextMethodMenu.SpriteName];
							NewSubMenu.Mono = mono;
							NewSubMenu.Method = method;

							newMenu.SubMenus.Add(NewSubMenu);
						}
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
			Menu[] array = new Menu[MenuOrder.Count + 1];
			List<Menu> AddEnd = new List<Menu>();


			foreach (Menu SubMenu in Sortlist)
			{
				if (MenuOrder.ContainsKey(SubMenu.title))
				{
					if (array[MenuOrder[SubMenu.title]] == null)
					{
						array[MenuOrder[SubMenu.title]] = SubMenu;
					}
					else
					{
						AddEnd.Add(SubMenu); //Quick fix need to think about it more
					}
				}
				else
				{
					AddEnd.Add(SubMenu); //Quick fix need to think about it more
				}
			}

			newMenu.SubMenus = new List<Menu>();

			for (int S = 0; S < array.Length; S++)
			{
				if (!(array[S] == null))
				{
					newMenu.SubMenus.Add(array[S]);
				}
			}

			newMenu.SubMenus.AddRange(AddEnd);
			ins.options.Add(newMenu);
		}
	}
}


[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class ContextMethod : Attribute
{
	public string colourHex;
	public string BGSpriteName;
	public InterColour InterColour;
	public string ToCheck;

	public string ButtonTitle;
	public string SpriteName;

	public ContextMethod(string ButtonTitle, string SpriteName, InterColour colourEnum = InterColour.Null,
		string colourHex = null, string BGSpriteName = null, string ToCheck = null)
	{
		this.ButtonTitle = ButtonTitle;
		this.SpriteName = SpriteName;
		this.colourHex = colourHex;
		this.InterColour = colourEnum;
		this.BGSpriteName = BGSpriteName;
		this.ToCheck = ToCheck;
	}
}


public class ButtonSettings
{
	public string ButtonTitle;
	public string SpriteName;
}

public enum InterColour
{
	Null,
	Default,
	Test,
};