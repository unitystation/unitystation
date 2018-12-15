using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadialMenu : MonoBehaviour {

	public Vector2 centercirlce = new Vector2(0.5f,0.5f);

	public Dictionary<int,RadialButton> ResetDepthOnDestroy = new Dictionary<int,RadialButton>();


	public Dictionary<int,List<RadialButton>> CurrentOptionsDepth = new Dictionary<int,List<RadialButton>>();

	public Dictionary<int,List<Rightclick.Menu>> DepthMenus = new Dictionary<int,List<Rightclick.Menu>>();

	public Dictionary<int,int> Density = new  Dictionary<int,int>(){
		{100,6},
		{200,15},
		{300,32},
		{400,64},
		{500,128}

	};

	public int CurrentMenuDepth;
	public RadialButton ButtonPrefab;

	public RadialButton Selected;

	public Vector2 MousePosition;
	public Vector2 toVector2M;

	public float LastInRangeSubMenu;

	public RadialButton LastSelected;


	public bool LastSelectedset = false;
	public bool Initialised = false;

	public Dictionary<int,List<float>> SelectionRange = new Dictionary<int,List<float>>();
	public int MenuItem;

	public float LastSelectedTime;

	public void SetupMenu (List<Rightclick.Menu> ListRightclick) {
		//Captures the centre circle
		centercirlce = new Vector2 (Input.mousePosition.x, Input.mousePosition.y);
		SpawnButtons (ListRightclick,100,0);

	}
	public void SpawnButtons (List<Rightclick.Menu> Menus,int Menudepth,int StartingAngle) {
		Initialised = false;
		CurrentMenuDepth = Menudepth;
		int Range = 360; //is the range that the buttons will be on in degrees
		int MinimumAngle = 0; //The initial offset Of the buttons in degrees
		int MaximumAngle = 360; //Linked to range
		if (Menudepth > 100) {
			Range = Menus.Count * (360 / Density [Menudepth]); //Try and keep the icons nicely spaced on the outer rings
			MinimumAngle = (int)(StartingAngle - ((Range / 2) - (0.5f * (360 / Density [Menudepth]))));
			MaximumAngle = StartingAngle + Range;

			if (Range < (SelectionRange [Menudepth - 100] [0] / SelectionRange [Menudepth - 100] [3])) {
				Range = (int)(SelectionRange [Menudepth - 100] [0] / SelectionRange [Menudepth - 100] [3]); //Try and keep the icons nicely spaced on the outer rings
				MinimumAngle = (int)(StartingAngle - ((Range / 2) - (0.5f * (Range / Menus.Count))));
				MaximumAngle = StartingAngle + Range;
			}
		}
	
		for (int i = 0; i < Menus.Count; i++) {
			RadialButton newButton = Instantiate (ButtonPrefab) as RadialButton;
			newButton.transform.SetParent (transform, false);
			//Magic maths
			float theta = (float)(((Range*Mathf.Deg2Rad) / Menus.Count) * i);
			theta = (theta + (MinimumAngle * Mathf.Deg2Rad));
			float xpos = Mathf.Sin (theta);
			float ypos = Mathf.Cos (theta);
			newButton.transform.localPosition = new Vector2 (xpos, ypos) * Menudepth;

			newButton.Circle.color = Menus[i].colour;
			newButton.Icon.sprite = Menus[i].sprite;
			newButton.Item = Menus [i].Item;
			newButton.MenuDepth = Menudepth;
			newButton.Mono = Menus[i].Mono;
			newButton.Method = Menus[i].Method;
			newButton.Hiddentitle = Menus[i].title;
			if (Menus [i].BackgroundSprite != null) {
				newButton.Circle.sprite = Menus[i].BackgroundSprite;
			}

			newButton.MyMenu = this;

			// Annoying dictionary not containing list when Initialised
			if (CurrentOptionsDepth.ContainsKey (Menudepth)) {
				CurrentOptionsDepth[Menudepth].Add (newButton);
			} else {
				CurrentOptionsDepth[Menudepth] = new List<RadialButton>();
				CurrentOptionsDepth[Menudepth].Add (newButton);
			}
			if (DepthMenus.ContainsKey (Menudepth)) {
				DepthMenus [Menudepth].Add (Menus [i]);
			} else {
				DepthMenus [Menudepth] = new List<Rightclick.Menu>();
				DepthMenus [Menudepth].Add (Menus [i]);
			}

		}
		//Pushes the parameters to the selection system
		List<float> QuickList = new List<float> {
			Range,MinimumAngle,MaximumAngle, Menus.Count
		};
		SelectionRange [Menudepth] = QuickList;
		Initialised = true;

	}

	void Update () {
		if (Initialised) {
			List<RadialButton> CurrentOptions = CurrentOptionsDepth [CurrentMenuDepth];

			MousePosition = new Vector2 (Input.mousePosition.x, Input.mousePosition.y);
			toVector2M = new Vector2 (Input.mousePosition.x, Input.mousePosition.y);
			double IndividualItemDegrees = 0;
			Vector2 Relativecentre = toVector2M - centercirlce;
			//Logger.Log (Relativecentre.ToString ()+ " Relativecentre" , Category.RightClick);
			double Angle = (Mathf.Atan2 (Relativecentre.y, Relativecentre.x) * Mathf.Rad2Deg);
			//off sets the Angle because it starts as -180 to 180
			Angle += -90;
			Angle = Angle + SelectionRange[CurrentMenuDepth][1];
			if (Angle > 0) {
				Angle += -360;
			}
			Angle = Angle * -1; //Turns it from negative to positive

			//Logger.Log (Angle.ToString () + " old Angle", Category.RightClick);
			//Logger.Log (((int)((Angle) / (SelectionRange[CurrentMenuDepth][0] / CurrentOptions.Count))).ToString () + " old MenuItem", Category.RightClick);
			//Logger.Log (Angle.ToString ()+ " Angle" , Category.RightClick);
			IndividualItemDegrees = SelectionRange[CurrentMenuDepth][0] / CurrentOptions.Count;
			Angle = Angle + ((IndividualItemDegrees) / 2); //Offsets by half a menu so So the different selection areas aren't in the middle of the menu

			if (Angle > 360) { //Makes sure it's 360
				Angle += -360;
			}

			MenuItem = (int)((Angle) / (IndividualItemDegrees)); 

			//Logger.Log ((IndividualItemDegrees).ToString () + " Density", Category.RightClick);
			//Logger.Log (Angle.ToString () + " Angle", Category.RightClick);
			//Logger.Log (SelectionRange[CurrentMenuDepth][0].ToString () + " Range", Category.RightClick);
			//Logger.Log (SelectionRange[CurrentMenuDepth][1].ToString () + " MinimumAngle", Category.RightClick);
			//Logger.Log (MenuItem.ToString () + "MenuItem", Category.RightClick);
			//Logger.Log (CurrentOptions.Count.ToString () + "CurrentOptions.Count", Category.RightClick);

			if (!(MenuItem > (CurrentOptions.Count - 1)) && !(MenuItem < 0)) { //Ensures its in range Of selection
				LastInRangeSubMenu = Time.time;
				Selected = CurrentOptions [MenuItem];
				if (!(LastSelected == Selected)) {
					if (LastSelectedset) {
						if (LastSelected.MenuDepth == CurrentMenuDepth) {
							LastSelected.title.text = "";
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
					if ((Time.time - LastSelectedTime) > 0.4f) { //How long it takes to make a menu

						if ((!(DepthMenus [CurrentMenuDepth] [MenuItem].SubMenus == null)) && DepthMenus [CurrentMenuDepth] [MenuItem].SubMenus.Count > 0) {
							Logger.Log (MenuItem.ToString () + " Selected", Category.RightClick);
							int NewMenuDepth = CurrentMenuDepth;
							LastSelectedTime = Time.time;
							NewMenuDepth = NewMenuDepth + 100;
							int InitialAngle = MenuItem * (360 / CurrentOptions.Count);

							SpawnButtons (DepthMenus [CurrentMenuDepth] [MenuItem].SubMenus, NewMenuDepth, InitialAngle);
						}
					}

				}
			} else {
				if ((Time.time - LastInRangeSubMenu) > 0.3f && (CurrentMenuDepth > 100)){ //How long it takes to exit a menu
					//Logger.Log ("yo am Destroying", Category.UI);

					if (ResetDepthOnDestroy.ContainsKey (CurrentMenuDepth))  {
						ResetDepthOnDestroy [CurrentMenuDepth].title.text = "";
						ResetDepthOnDestroy [CurrentMenuDepth].transform.SetSiblingIndex (ResetDepthOnDestroy [CurrentMenuDepth].DefaultPosition);
						ResetDepthOnDestroy [CurrentMenuDepth].SetColour (ResetDepthOnDestroy [CurrentMenuDepth].DefaultColour);
					} else {
						LastSelected.transform.SetSiblingIndex(LastSelected.DefaultPosition);
						LastSelected.SetColour(LastSelected.DefaultColour);
						LastSelected.title.text = "";
						LastSelected = null;
						LastSelectedset = false;
					}
					List<RadialButton> Acopy = CurrentOptions;
					for (int i = 0; i < Acopy.Count; i++) {
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
		if (Input.GetMouseButtonUp (1))
		{
			if (Selected) {
				if (!(Selected.Mono == null)) {
					//The magic function
					Selected.Method.Invoke(Selected.Mono, new object[] {  });
				}
				//Logger.Log ("yo this "+Selected.title.text , Category.RightClick);
			}
			LastSelectedset = false;
			Destroy (gameObject);
		}
	}
}

