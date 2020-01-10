using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadialMenu : MonoBehaviour
{

	private List<RadialButton> topLevelButtons = new List<RadialButton>();

	public Vector2 centercirlce = new Vector2(0.5f, 0.5f);

	public Dictionary<int, RadialButton> ResetDepthOnDestroy = new Dictionary<int, RadialButton>();

	public Dictionary<int, List<RadialButton>> CurrentOptionsDepth = new Dictionary<int, List<RadialButton>>();

	public Dictionary<int, List<RightClickMenuItem>> DepthMenus = new Dictionary<int, List<RightClickMenuItem>>();

	public Dictionary<int, int> Density = new Dictionary<int, int>()
	{
		{100, 6},
		{200, 15},
		{300, 32},
		{400, 64},
		{500, 128}
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

	public Dictionary<int, List<float>> SelectionRange = new Dictionary<int, List<float>>();
	public int MenuItem;

	public float LastSelectedTime;

	public void SetupMenu(List<RightClickMenuItem> ListRightclick)
	{
		//Captures the centre circle
		centercirlce = new Vector2(CommonInput.mousePosition.x, CommonInput.mousePosition.y);
		SpawnButtons(ListRightclick, 100, 0);
	}

	public void SpawnButtons(List<RightClickMenuItem> menus, int menudepth, int startingAngle)
	{
		Initialised = false;
		CurrentMenuDepth = menudepth;
		int range = 360; //is the range that the buttons will be on in degrees
		int minimumAngle = 0; //The initial offset Of the buttons in degrees
		int maximumAngle = 360; //Linked to range

		if (menudepth > 100)
		{
			range = menus.Count * (360 / Density[menudepth]); //Try and keep the icons nicely spaced on the outer rings
			minimumAngle = (int) (startingAngle - ((range / 2) - (0.5f * (360 / Density[menudepth]))));
			maximumAngle = startingAngle + range;

			if (range < (SelectionRange[menudepth - 100][0] / SelectionRange[menudepth - 100][3]))
			{
				range = (int) (SelectionRange[menudepth - 100][0] /
				               SelectionRange[menudepth - 100][3]
					); //Try and keep the icons nicely spaced on the outer rings
				minimumAngle = (int) (startingAngle - ((range / 2) - (0.5f * (range / menus.Count))));
				maximumAngle = startingAngle + range;
			}
		}

		for (var i = 0; i < menus.Count; i++)
		{
			RadialButton newButton = Instantiate(ButtonPrefab) as RadialButton;
			newButton.transform.SetParent(transform, false);
			//Magic maths
			float theta = (float) (((range * Mathf.Deg2Rad) / menus.Count) * i);
			theta = (theta + (minimumAngle * Mathf.Deg2Rad));
			float xpos = Mathf.Sin(theta);
			float ypos = Mathf.Cos(theta);

			newButton.SetButton(new Vector2(xpos, ypos) * menudepth, this, menus[i], true);

			topLevelButtons.Add(newButton);

//			// Annoying dictionary not containing list when Initialised
//			if (CurrentOptionsDepth.ContainsKey(menudepth))
//			{
//				CurrentOptionsDepth[menudepth].Add(newButton);
//			}
//			else
//			{
//				CurrentOptionsDepth[menudepth] = new List<RadialButton>();
//				CurrentOptionsDepth[menudepth].Add(newButton);
//			}
//
//			if (DepthMenus.ContainsKey(menudepth))
//			{
//				DepthMenus[menudepth].Add(menus[i]);
//			}
//			else
//			{
//				DepthMenus[menudepth] = new List<RightClickMenuItem>();
//				DepthMenus[menudepth].Add(menus[i]);
//			}
		}

		//Pushes the parameters to the selection system
		List<float> QuickList = new List<float>
		{
			range, minimumAngle, maximumAngle, menus.Count
		};
		SelectionRange[menudepth] = QuickList;
		Initialised = true;
	}

	void Update()
	{
//		if (Initialised)
//		{
//			List<RadialButton> CurrentOptions = CurrentOptionsDepth[CurrentMenuDepth];
//
//			MousePosition = new Vector2(CommonInput.mousePosition.x, CommonInput.mousePosition.y);
//			toVector2M = new Vector2(CommonInput.mousePosition.x, CommonInput.mousePosition.y);
//			double IndividualItemDegrees = 0;
//			Vector2 Relativecentre = toVector2M - centercirlce;
//			//Logger.Log (Relativecentre.ToString ()+ " Relativecentre" , Category.RightClick);
//			double Angle = (Mathf.Atan2(Relativecentre.y, Relativecentre.x) * Mathf.Rad2Deg);
//			//off sets the Angle because it starts as -180 to 180
//			Angle += -90;
//			Angle = Angle + SelectionRange[CurrentMenuDepth][1];
//			if (Angle > 0)
//			{
//				Angle += -360;
//			}
//
//			Angle = Angle * -1; //Turns it from negative to positive
//
//			IndividualItemDegrees = SelectionRange[CurrentMenuDepth][0] / CurrentOptions.Count;
//			Angle = Angle + ((IndividualItemDegrees) /
//			                 2); //Offsets by half a menu so So the different selection areas aren't in the middle of the menu
//
//			if (Angle > 360)
//			{
//				//Makes sure it's 360
//				Angle += -360;
//			}
//
//			MenuItem = (int) ((Angle) / (IndividualItemDegrees));
//
//			if (!(MenuItem > (CurrentOptions.Count - 1)) && !(MenuItem < 0))
//			{
//				//Ensures its in range Of selection
//				LastInRangeSubMenu = Time.time;
//				Selected = CurrentOptions[MenuItem];
//				if (!(LastSelected == Selected))
//				{
//					if (LastSelectedset)
//					{
//						if (LastSelected.MenuDepth == CurrentMenuDepth)
//						{
//							LastSelected.title.text = "";
//							LastSelected.transform.SetSiblingIndex(LastSelected.DefaultPosition);
//							LastSelected.SetColour(LastSelected.DefaultColour);
//						}
//						else
//						{
//							ResetDepthOnDestroy[CurrentMenuDepth] = LastSelected;
//						}
//					}
//
//					CurrentOptions[MenuItem].title.text = CurrentOptions[MenuItem].Hiddentitle;
//					CurrentOptions[MenuItem].DefaultColour = CurrentOptions[MenuItem].ReceiveCurrentColour();
//					CurrentOptions[MenuItem].DefaultPosition = CurrentOptions[MenuItem].transform.GetSiblingIndex();
//					CurrentOptions[MenuItem].SetColour(CurrentOptions[MenuItem].DefaultColour + (Color.white / 3f));
//					CurrentOptions[MenuItem].transform.SetAsLastSibling();
//					LastSelected = CurrentOptions[MenuItem];
//					LastSelectedset = true;
//					LastSelectedTime = Time.time;
//					//Logger.Log (LastSelectedTime.ToString (), Category.RightClick);
//				}
//
//				if (LastSelectedset)
//				{
//					if ((Time.time - LastSelectedTime) > 0.4f)
//					{
//						//How long it takes to make a menu
//
//						if ((!(DepthMenus[CurrentMenuDepth][MenuItem].SubMenus == null)) &&
//						    DepthMenus[CurrentMenuDepth][MenuItem].SubMenus.Count > 0)
//						{
//							Logger.Log(MenuItem.ToString() + " Selected", Category.RightClick);
//							int NewMenuDepth = CurrentMenuDepth;
//							LastSelectedTime = Time.time;
//							NewMenuDepth = NewMenuDepth + 100;
//							int InitialAngle = MenuItem * (360 / CurrentOptions.Count);
//
//							SpawnButtons(DepthMenus[CurrentMenuDepth][MenuItem].SubMenus, NewMenuDepth, InitialAngle);
//						}
//					}
//				}
//			}
//			else
//			{
//				if ((Time.time - LastInRangeSubMenu) > 0.3f && (CurrentMenuDepth > 100))
//				{
//					//How long it takes to exit a menu
//					//Logger.Log ("yo am Destroying", Category.UI);
//
//					if (ResetDepthOnDestroy.ContainsKey(CurrentMenuDepth))
//					{
//						ResetDepthOnDestroy[CurrentMenuDepth].title.text = "";
//						ResetDepthOnDestroy[CurrentMenuDepth].transform
//							.SetSiblingIndex(ResetDepthOnDestroy[CurrentMenuDepth].DefaultPosition);
//						ResetDepthOnDestroy[CurrentMenuDepth]
//							.SetColour(ResetDepthOnDestroy[CurrentMenuDepth].DefaultColour);
//					}
//					else
//					{
//						LastSelected.transform.SetSiblingIndex(LastSelected.DefaultPosition);
//						LastSelected.SetColour(LastSelected.DefaultColour);
//						LastSelected.title.text = "";
//						LastSelected = null;
//						LastSelectedset = false;
//					}
//
//					List<RadialButton> Acopy = CurrentOptions;
//					for (int i = 0; i < Acopy.Count; i++)
//					{
//						Destroy(CurrentOptions[i].gameObject);
//					}
//
//					//Cleans up the dictionarys
//					SelectionRange.Remove(CurrentMenuDepth);
//					CurrentOptionsDepth.Remove(CurrentMenuDepth);
//					DepthMenus.Remove(CurrentMenuDepth);
//					ResetDepthOnDestroy.Remove(CurrentMenuDepth);
//					CurrentMenuDepth = CurrentMenuDepth - 100;
//					LastSelectedset = false;
//				}
//			}

		if (CommonInput.GetMouseButtonUp(1))
		{
//			if (Selected)
//			{
//				Selected.Action?.Invoke();
//				//Logger.Log ("yo this "+Selected.title.text , Category.RightClick);
//			}

			LastSelectedset = false;
			Destroy(gameObject);
		}
	}

	public void SetButtonAsLastSibling(RadialButton radialButton)
	{
		int index = topLevelButtons.IndexOf(radialButton);

		for (int i = index; i >= 0; i--)
		{
			topLevelButtons[i].transform.SetAsFirstSibling();
		}

		for (int i = topLevelButtons.Count - 1; i > index; i--)
		{
			topLevelButtons[i].transform.SetAsFirstSibling();
		}
	}
}