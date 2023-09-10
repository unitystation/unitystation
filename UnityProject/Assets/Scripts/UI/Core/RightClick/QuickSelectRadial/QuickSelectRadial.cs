using System;
using System.Collections.Generic;
using Logs;
using UI.Core;
using UI.Core.RightClick;
using UnityEngine;

public class QuickSelectRadial : MonoBehaviour, IRightClickMenu
{

	public Vector2 centercirlce = new Vector2(0.5f,0.5f);

	private GameObject self;
	GameObject IRightClickMenu.Self
	{
		get => self == null ? gameObject : self;
		set => self = value;
	}

	public List<RightClickMenuItem> Items { get; set; }

	public Dictionary<int,QuickRadialButton> ResetDepthOnDestroy = new Dictionary<int,QuickRadialButton>();

	public Dictionary<int,List<QuickRadialButton>> CurrentOptionsDepth = new Dictionary<int,List<QuickRadialButton>>();

	public Dictionary<int,List<RightClickMenuItem>> DepthMenus = new Dictionary<int,List<RightClickMenuItem>>();

	public Dictionary<int,int> Density = new  Dictionary<int,int>(){
		{100,6},
		{200,15},
		{300,32},
		{400,64},
		{500,128}

	};

	public List<QuickRadialButton> SpawnedButtons = new List<QuickRadialButton>();

	public int CurrentMenuDepth;
	public QuickRadialButton ButtonPrefab;

	public QuickRadialButton Selected;

	public Vector2 MousePosition;
	public Vector2 toVector2M;

	public float LastInRangeSubMenu;

	public QuickRadialButton LastSelected;


	public bool LastSelectedset = false;
	public bool Initialised = false;

	public Dictionary<int,SelectionData> SelectionRange = new Dictionary<int,SelectionData>();

	public struct SelectionData
	{
		public float Range { get; set; }
		public float MinimumAngle { get; set; }
		public float MaximumAngle { get; set; }
		public float NumberOfMenus { get; set; }
	}


	public int MenuItem;

	public float LastSelectedTime;

	public void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	public void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}


	public void SetupMenu(List<RightClickMenuItem> items, IRadialPosition radialPosition,
		RightClickRadialOptions radialOptions)
	{
		gameObject.SetActive(true);
		Items = items;
		//Captures the centre circle
		this.gameObject.transform.position = radialPosition.GetPositionIn(Camera.main, UIManager.Instance.GetComponent<Canvas>());
		centercirlce = new Vector2 (CommonInput.mousePosition.x, CommonInput.mousePosition.y);

		LastSelectedset = false;
		Selected = null;
		SelectionRange.Clear();
		DepthMenus.Clear();
		CurrentOptionsDepth.Clear();
		ResetDepthOnDestroy.Clear();
		foreach (var Button in SpawnedButtons)
		{
			Destroy(Button.gameObject);
		}
		SpawnedButtons.Clear();
		CurrentMenuDepth = 0;

		if (items.Count == 0)
		{
			gameObject.SetActive(false);
		}


		SpawnButtons (items,100,0);
	}
	public void SpawnButtons (List<RightClickMenuItem> Menus,int Menudepth,int StartingAngle) {
		if (gameObject.activeInHierarchy == false) return;

		Loggy.Log (StartingAngle.ToString ()+ " StartingAngle" );

		Initialised = false;
		CurrentMenuDepth = Menudepth;
		float Range = 360; //is the range that the buttons will be on in degrees
		float MinimumAngle = 0; //The initial offset Of the buttons in degrees
		float MaximumAngle = 360;
		//Linked to range
		if (Menudepth > 100) {

			//- (0.5f * (360f / Density[Menudepth]))));
			Range = Menus.Count * (360f / Density[Menudepth]); //Try and keep the icons nicely spaced on the outer rings
			MinimumAngle = StartingAngle - (Range / 2f);
			MaximumAngle = StartingAngle + (Range / 2f);

			if (Range < (SelectionRange [Menudepth - 100].Range / SelectionRange [Menudepth - 100].NumberOfMenus)) {

				Loggy.LogError("AAAAA");
				Range = (SelectionRange [Menudepth - 100].Range  / SelectionRange [Menudepth - 100].NumberOfMenus);

				//Try and keep the icons nicely spaced on the outer rings
				MinimumAngle = StartingAngle - (Range / 2f);

				MaximumAngle = StartingAngle + (Range / 2f);
			}
		}



		Loggy.LogError("MinimumAngle" + MinimumAngle);
		Loggy.LogError("MaximumAngle" + MaximumAngle);

		var RadIncrement = (Range / Menus.Count) * Mathf.Deg2Rad;
		var radMinimumAngle = (MinimumAngle * Mathf.Deg2Rad);

		for (var i = 0; i < Menus.Count; i++) {
			QuickRadialButton newButton = Instantiate (ButtonPrefab) as QuickRadialButton;
			newButton.transform.SetParent (transform, false);
			SpawnedButtons.Add(newButton);
			//Magic maths

			float theta = RadIncrement * i;

			theta = (theta + radMinimumAngle);

			Loggy.LogError("theta " + theta);

			float xpos = Mathf.Sin (theta);
			float ypos = Mathf.Cos (theta);

			newButton.transform.localPosition = new Vector2 (xpos, ypos) * Menudepth;

			newButton.Circle.color = Menus[i].BackgroundColor;
			newButton.Icon.sprite  = Menus[i].IconSprite;
			newButton.Icon.ApplySpriteScaling(newButton.Icon.sprite);
			if (Menus[i].BackgroundSprite != null)
			{
				newButton.Circle.sprite = Menus[i].BackgroundSprite;
			}

			newButton.MenuDepth = Menudepth;
			newButton.Action = Menus[i].Action;
			newButton.Hiddentitle = Menus[i].Label;

			newButton.AcompanyingButtons = Menus.Count;
			if (newButton.ShouldHideLabel == false)
			{
				newButton.title.text = Menus[i].Label;
			}


			newButton.MyMenu = this;

			// Annoying dictionary not containing list when Initialised
			if (CurrentOptionsDepth.ContainsKey (Menudepth)) {
				CurrentOptionsDepth[Menudepth].Add (newButton);
			} else {
				CurrentOptionsDepth[Menudepth] = new List<QuickRadialButton>();
				CurrentOptionsDepth[Menudepth].Add (newButton);
			}
			if (DepthMenus.ContainsKey (Menudepth)) {
				DepthMenus [Menudepth].Add (Menus [i]);
			} else {
				DepthMenus [Menudepth] = new List<RightClickMenuItem>();
				DepthMenus [Menudepth].Add (Menus [i]);
			}

		}
		//Pushes the parameters to the selection system
		SelectionData QuickList = new SelectionData {
			Range = Range,MinimumAngle = MinimumAngle,MaximumAngle = MaximumAngle,NumberOfMenus = Menus.Count
		};
		SelectionRange [Menudepth] = QuickList;
		Initialised = true;


	}

	void UpdateMe () {
		if (Initialised) {
			if (CurrentOptionsDepth.ContainsKey(CurrentMenuDepth) == false)
			{
				gameObject.SetActive(false);
				return;
			}
			List<QuickRadialButton> CurrentOptions = CurrentOptionsDepth [CurrentMenuDepth];

			MousePosition = new Vector2 (CommonInput.mousePosition.x, CommonInput.mousePosition.y);
			toVector2M = new Vector2 (CommonInput.mousePosition.x, CommonInput.mousePosition.y);
			float IndividualItemDegrees = 0;
			Vector2 Relativecentre = toVector2M - centercirlce;
			//Loggy.Log (Relativecentre.ToString ()+ " Relativecentre");
			float Angle = (Mathf.Atan2 (Relativecentre.y, Relativecentre.x) * Mathf.Rad2Deg);
			//off sets the Angle because it starts as -180 to 180
			Angle += -90;

			Angle = Angle + SelectionRange[CurrentMenuDepth].MinimumAngle;
			if (Angle > 0) {
				Angle += -360;
			}
			Angle = Angle * -1; //Turns it from negative to positive

			//Loggy.Log (Angle.ToString () + " old Angle");
			//Loggy.Log (((int)((Angle) / (SelectionRange[CurrentMenuDepth][0] / CurrentOptions.Count))).ToString () + " old MenuItem");
			//Loggy.Log (Angle.ToString ()+ " Angle" );

			IndividualItemDegrees = SelectionRange[CurrentMenuDepth].Range / CurrentOptions.Count;
			Angle = Angle + ((IndividualItemDegrees) / 2); //Offsets by half a menu so So the different selection areas aren't in the middle of the menu

			if (Angle > 360) { //Makes sure it's 360
				Angle += -360;
			}

			//Loggy.LogError("IndividualItemDegrees > " + IndividualItemDegrees);

			MenuItem = Mathf.RoundToInt(((Angle) / (IndividualItemDegrees)));

			//Loggy.Log ((IndividualItemDegrees).ToString () + " Density");
			//Loggy.Log (Angle.ToString () + " Angle");
			//Loggy.Log (SelectionRange[CurrentMenuDepth][0].ToString () + " Range");
			//Loggy.Log (SelectionRange[CurrentMenuDepth][1].ToString () + " MinimumAngle");
			//Loggy.Log (MenuItem.ToString () + "MenuItem");
			//Loggy.Log (CurrentOptions.Count.ToString () + "CurrentOptions.Count");

			if (!(MenuItem > (CurrentOptions.Count - 1)) && !(MenuItem < 0)) { //Ensures its in range Of selection
				LastInRangeSubMenu = Time.time;
				Selected = CurrentOptions[MenuItem];
				if (!(LastSelected == Selected)) {
					if (LastSelectedset) {
						if (LastSelected.MenuDepth == CurrentMenuDepth) {

							if (LastSelected.ShouldHideLabel)
							{
								LastSelected.title.text = "";
							}

							LastSelected.transform.SetSiblingIndex (LastSelected.DefaultPosition);
							LastSelected.SetColour (LastSelected.DefaultColour);
						} else {
							ResetDepthOnDestroy [CurrentMenuDepth] = LastSelected;
						}
					}
					CurrentOptions [MenuItem].title.text = CurrentOptions [MenuItem].Hiddentitle;
					CurrentOptions [MenuItem].DefaultColour = CurrentOptions [MenuItem].ReceiveCurrentColour ();
					CurrentOptions [MenuItem].DefaultPosition = CurrentOptions[MenuItem].transform.GetSiblingIndex();
					CurrentOptions [MenuItem].SetColour (CurrentOptions [MenuItem].DefaultColour + (Color.white / 3f));
					CurrentOptions [MenuItem].transform.SetAsLastSibling();
					LastSelected = CurrentOptions [MenuItem];
					LastSelectedset = true;
					LastSelectedTime = Time.time;
					//Logger.Log (LastSelectedTime.ToString (), Category.RightClick);
				}
				if (LastSelectedset) {

					if (KeyboardInputManager.Instance.CheckKeyAction(KeyAction.PreventRadialQuickSelectOpen, KeyboardInputManager.KeyEventType.Hold) == false
					    && (Time.time - LastSelectedTime) > 0.4f) { //How long it takes to make a menu

						if ((!(DepthMenus [CurrentMenuDepth] [MenuItem].SubMenus == null)) && DepthMenus [CurrentMenuDepth] [MenuItem].SubMenus.Count > 0) {
							Loggy.Log (MenuItem.ToString () + " Selected", Category.UserInput);
							int NewMenuDepth = CurrentMenuDepth;
							LastSelectedTime = Time.time;
							NewMenuDepth = NewMenuDepth + 100;
							int InitialAngle = MenuItem * (360 / CurrentOptions.Count);

							SpawnButtons (DepthMenus [CurrentMenuDepth] [MenuItem].SubMenus, NewMenuDepth, InitialAngle);
						}
					}

				}
			} else {
				if ((Time.time - LastInRangeSubMenu) > 0.1f && (CurrentMenuDepth > 100)){ //How long it takes to exit a menu
					//Logger.Log ("yo am Destroying", Category.UI);

					if (ResetDepthOnDestroy.ContainsKey (CurrentMenuDepth))  {
						if (ResetDepthOnDestroy [CurrentMenuDepth].ShouldHideLabel)
						{
							ResetDepthOnDestroy [CurrentMenuDepth].title.text = "";
						}

						ResetDepthOnDestroy [CurrentMenuDepth].transform.SetSiblingIndex (ResetDepthOnDestroy [CurrentMenuDepth].DefaultPosition);
						ResetDepthOnDestroy [CurrentMenuDepth].SetColour (ResetDepthOnDestroy [CurrentMenuDepth].DefaultColour);
					} else {
						LastSelected.transform.SetSiblingIndex(LastSelected.DefaultPosition);
						LastSelected.SetColour(LastSelected.DefaultColour);

						if (LastSelected.ShouldHideLabel)
						{
							LastSelected.title.text = "";
						}
						LastSelected = null;
						LastSelectedset = false;
					}
					List<QuickRadialButton> Acopy = CurrentOptions;
					for (int i = 0; i < Acopy.Count; i++)
					{
						SpawnedButtons.Remove(CurrentOptions[i]);
						Destroy (CurrentOptions[i].gameObject);
					}
					//Cleans up the dictionarys
					SelectionRange.Remove (CurrentMenuDepth);
					CurrentOptionsDepth.Remove (CurrentMenuDepth);
					DepthMenus.Remove (CurrentMenuDepth);
					ResetDepthOnDestroy.Remove (CurrentMenuDepth);
					CurrentMenuDepth = CurrentMenuDepth - 100;
					LastSelectedset = false;
				}
			}
		}
		if (CommonInput.GetMouseButtonUp(1))
		{
			if (Selected)
			{
				Selected.Action?.Invoke();
				//Logger.Log ("yo this "+Selected.title.text , Category.RightClick);
			}
			LastSelectedset = false;
			Selected = null;
			SelectionRange.Clear();
			DepthMenus.Clear();
			CurrentOptionsDepth.Clear();
			ResetDepthOnDestroy.Clear();
			foreach (var Button in SpawnedButtons)
			{
				Destroy(Button.gameObject);
			}
			SpawnedButtons.Clear();
			CurrentMenuDepth = 0;
			gameObject.SetActive(false);
		}
	}
}