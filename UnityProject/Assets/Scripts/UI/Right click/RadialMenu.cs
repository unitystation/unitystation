using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class RadialMenu : MonoBehaviour, IPointerEnterHandler
{
	private List<RadialButton> topLevelButtons = new List<RadialButton>();

	public Dictionary<int, int> density = new Dictionary<int, int>()
	{
		{100, 6},
		{200, 15},
		{300, 32},
		{400, 64},
		{500, 128}
	};

	public RadialButton ButtonPrefab;

	public RadialButton Selected;

	public void SetupMenu(List<RightClickMenuItem> ListRightclick)
	{
		SpawnButtons(ListRightclick, 100, 0);
	}

	public void SpawnButtons(List<RightClickMenuItem> menus, int menudepth, int startingAngle, int topLevelParent = -1)
	{
		int range = 360; //is the range that the buttons will be on in degrees
		int minimumAngle = 0; //The initial offset Of the buttons in degrees
		int maximumAngle = 360; //Linked to range

		if (menudepth > 100)
		{
			range = menus.Count * (360 / density[menudepth]); //Try and keep the icons nicely spaced on the outer rings
			minimumAngle = (int) (startingAngle - ((range / 2) - (0.5f * (360 / density[menudepth]))));
			maximumAngle = startingAngle + range;
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

			if (menudepth == 100)
			{
				topLevelButtons.Add(newButton);
				newButton.SetButton(new Vector2(xpos, ypos) * menudepth, this, menus[i], true);
			}
			else if(menudepth == 200)
			{
				newButton.SetButton(new Vector2(xpos, ypos) * menudepth, this, menus[i], false);
				topLevelButtons[topLevelParent].childButtons.Add(newButton);
			}
			else
			{
				//TODO: What to do with higher depths ?!?! probably just keep it at 2 layers for time being
			}

			if (menus[i].SubMenus != null)
			{
				if (menus[i].SubMenus.Count != 0)
				{
					Vector2 targetDir = newButton.transform.position - transform.position;
					var angle = Vector2.Angle(targetDir, transform.up);
					if (targetDir.x < 0) angle *= -1;

					SpawnButtons(menus[i].SubMenus, menudepth + 100, (int) angle, i);
				}
			}

			foreach (var btn in newButton.childButtons)
			{
				btn.gameObject.SetActive(false);
			}
		}
	}

	void Update()
	{
		if (CommonInput.GetMouseButtonUp(1))
		{
			if (Selected)
			{
				Selected.action?.Invoke();
			}

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

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (eventData.pointerEnter != gameObject) return;

		SelectTopLevelButton(null);
	}

	public void SelectTopLevelButton(RadialButton button)
	{
		var index = -1;
		if (button != null)
		{
			index = topLevelButtons.IndexOf(button);
		}

		for (int i = 0; i < topLevelButtons.Count; i++)
		{
			if (i == index)
			{
				topLevelButtons[i].TopLevelSelectToggle(true);
			}
			else
			{
				topLevelButtons[i].TopLevelSelectToggle(false);
			}
		}
	}
}