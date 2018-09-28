using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadialMenu : MonoBehaviour {

	public Rightclick StoredObject;
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

	public RadialButton LastSelected; //Unity can be seen crying its eyes out because using an everyday c# command Is apparently not allowed


	public bool LastSelectedset = false;
	public bool Initialised = false;

	public Dictionary<int,List<int>> SelectionRange = new Dictionary<int,List<int>>(); 
	public int MenuItem;

	public float LastSelectedTime;

	//public List<RadialButton> CurrentOptions;
	public bool CapturedCentre = false;

	// Use this for initialization
	public void SetupMenu (Rightclick obj) {
		centercirlce = new Vector2 (Input.mousePosition.x, Input.mousePosition.y);
		StoredObject = obj;
		SpawnButtons (obj.options,100,0);

	}
	public void SpawnButtons (List<Rightclick.Menu> Menus,int Menudepth,int StartingAngle) {
		Initialised = false;
		CurrentMenuDepth = Menudepth;
		int Range = 360;
		int MinimumAngle = 0;
		int MaximumAngle = 360;
		if (Menudepth > 100) {
			Range = Menus.Count * (360 / Density [Menudepth]);
			MinimumAngle = (int) (StartingAngle - ((Range/2) - (0.5f * (360 / Density [Menudepth]))));
			MaximumAngle = StartingAngle + Range;
		} 
		for (int i = 0; i < Menus.Count; i++) {
			RadialButton newButton = Instantiate (ButtonPrefab) as RadialButton;
			newButton.transform.SetParent (transform, false);
			//Logger.Log((Range*Mathf.Deg2Rad).ToString() + "man", Category.UI);
			float theta = (float)(((Range*Mathf.Deg2Rad) / Menus.Count) * i);
			//float theta = (2 * Mathf.PI / Menus.Count) * i;
			theta = (theta + (MinimumAngle * Mathf.Deg2Rad));
			float xpos = Mathf.Sin (theta);
			float ypos = Mathf.Cos (theta);
			newButton.transform.localPosition = new Vector2 (xpos, ypos) * Menudepth; 
			//newButton.transform.localPosition = new Vector2 (0f, 100f);
			newButton.Circle.color = Menus[i].colour;
			newButton.Icon.sprite = Menus[i].sprite;
			newButton.FunctionType = Menus [i].FunctionType;
			newButton.Item = Menus [i].Item;
			newButton.MenuDepth = Menudepth;
			newButton.Mono = Menus[i].Mono;
			newButton.Method = Menus[i].Method;
			newButton.Hiddentitle = Menus[i].title;
			newButton.MyMenu = this;
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

			//Logger.Log ("yo added this", Category.UI);
		}
		List<int> QuickList = new List<int> {
			Range,MinimumAngle,MaximumAngle
		};
		SelectionRange [Menudepth] = QuickList;
		Initialised = true;

	}

	void Update () {
		if (Initialised) {
			//Logger.Log (CurrentMenuDepth.ToString (), Category.UI);
			List<RadialButton> CurrentOptions = CurrentOptionsDepth [CurrentMenuDepth];

			MousePosition = new Vector2 (Input.mousePosition.x, Input.mousePosition.y);
			toVector2M = new Vector2 (Input.mousePosition.x, Input.mousePosition.y);

			Vector2 Relativecentre = toVector2M - centercirlce;
			//Logger.Log (Relativecentre.ToString ()+ " Relativecentre" , Category.UI);
			float Angle = (Mathf.Atan2 (Relativecentre.y, Relativecentre.x) * Mathf.Rad2Deg);

			Angle += -90;
			//Angle += 180;
			Angle = Angle + SelectionRange[CurrentMenuDepth][1];
			if (Angle > 0) {
				Angle += -360;
			}
			Angle = Angle * -1;

			//Logger.Log (Angle.ToString () + " old Angle", Category.UI);
			//Logger.Log (((int)((Angle) / (SelectionRange[CurrentMenuDepth][0] / CurrentOptions.Count))).ToString () + " old MenuItem", Category.UI);
			//Logger.Log (Angle.ToString ()+ " Angle" , Category.UI);

			Angle = Angle + ((SelectionRange[CurrentMenuDepth][0] / CurrentOptions.Count) / 2);


			//Angle = Angle + SelectionRange[CurrentMenuDepth][1];
			if (Angle > 360) {
				Angle += -360;
			} 
			//Logger.Log (Angle.ToString ()+ " Angle" , Category.UI);
			//MenuItem = (int)(Angle / (360 / CurrentOptions.Count));
			//SelectionRange[CurrentMenuDepth][1] 
			MenuItem = (int)((Angle) / (SelectionRange[CurrentMenuDepth][0] / CurrentOptions.Count));

			//Logger.Log ((SelectionRange[CurrentMenuDepth][0] / CurrentOptions.Count).ToString () + " Density", Category.UI);
			//Logger.Log (Angle.ToString () + " Angle", Category.UI);
			//Logger.Log (SelectionRange[CurrentMenuDepth][0].ToString () + " Range", Category.UI);
			//Logger.Log (SelectionRange[CurrentMenuDepth][1].ToString () + " MinimumAngle", Category.UI);
			//Logger.Log (MenuItem.ToString () + "MenuItem", Category.UI);
			//Logger.Log (CurrentOptions.Count.ToString () + "CurrentOptions.Count", Category.UI);
			if (!(MenuItem > (CurrentOptions.Count - 1)) && !(MenuItem < 0)) {
				LastInRangeSubMenu = Time.time;
				Selected = CurrentOptions [MenuItem];

				if (!(LastSelected == Selected)) {
					if (LastSelectedset) {
						//LastSelected.SetColour (LastSelected.DefaultColour);
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
					CurrentOptions [MenuItem].SetColour (Color.white);
					CurrentOptions [MenuItem].transform.SetAsLastSibling();
					LastSelected = CurrentOptions [MenuItem];

					LastSelectedset = true;
					LastSelectedTime = Time.time;
					//Logger.Log (LastSelectedTime.ToString (), Category.UI);
				}
				if (LastSelectedset) {
					if ((Time.time - LastSelectedTime) > 0.4f) {
						
						if ((!(DepthMenus [CurrentMenuDepth] [MenuItem].SubMenus == null)) && DepthMenus [CurrentMenuDepth] [MenuItem].SubMenus.Count > 0) {
							Logger.Log (MenuItem.ToString () + " Selected", Category.UI);
							int NewMenuDepth = CurrentMenuDepth;
							LastSelectedTime = Time.time;
							NewMenuDepth = NewMenuDepth + 100;
							int InitialAngle = MenuItem * (360 / CurrentOptions.Count);

							//Logger.Log (InitialAngle.ToString () + "WOW", Category.UI);
							SpawnButtons (DepthMenus [CurrentMenuDepth] [MenuItem].SubMenus, NewMenuDepth, InitialAngle);
						}
					}

				}
			

			} else {
				if ((Time.time - LastInRangeSubMenu) > 0.3f && (CurrentMenuDepth > 100)){

					Logger.Log ("yo am Destroying", Category.UI);
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
				Logger.Log (Selected.FunctionType, Category.UI);
				if (!(Selected.Mono == null)) {
					Selected.Method.Invoke(Selected.Mono, new object[] {  });
				}
				Logger.Log ("yo this "+Selected.title.text , Category.UI);
			}
			//CurrentOptions = null;
			LastSelectedset = false;
			Destroy (gameObject);
		}
	}

}

