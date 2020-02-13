using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class UIActionManager : MonoBehaviour
{
	public GameObject Panel;

	private static UIActionManager uIActionManager;
	public static UIActionManager Instance
	{
		get
		{
			if (!uIActionManager)
			{
				uIActionManager = FindObjectOfType<UIActionManager>();
			}

			return uIActionManager;
		}
	}


	public UIAction UIAction;
	public List<UIAction> PooledUIAction = new List<UIAction>();

	public Dictionary<IActionGUI, UIAction> DicIActionGUI = new Dictionary<IActionGUI, UIAction>();


	public void SetAction(IActionGUI iActionGUI, bool Add, GameObject recipient = null)
	{

		if (Add)
		{
			if (DicIActionGUI.ContainsKey(iActionGUI))
			{
				Logger.Log("iActionGUI Already added", Category.UI);
				return;
			}
			AddAction(iActionGUI);
		}
		else {
			RemoveAction(iActionGUI);
		}
	}


	public void SetSprite(IActionGUI iActionGUI, Sprite sprite)
	{
		if (DicIActionGUI.ContainsKey(iActionGUI))
		{
			var _UIAction = DicIActionGUI[iActionGUI];
			_UIAction.IconFront.SetSprite(sprite);
		}
		else {
			Logger.Log("iActionGUI Not present", Category.UI);
		}
	}



	public void SetSprite(IActionGUI iActionGUI, int Location)
	{
		if (DicIActionGUI.ContainsKey(iActionGUI))
		{
			var _UIAction = DicIActionGUI[iActionGUI];
			_UIAction.IconFront.ChangeSpriteVariant(Location);
		}
		else {

			Logger.Log("iActionGUI Not present", Category.UI);
		}
	}

	public void SetBackground(IActionGUI iActionGUI, int Location)
	{
		if (DicIActionGUI.ContainsKey(iActionGUI))
		{
			var _UIAction = DicIActionGUI[iActionGUI];
			_UIAction.IconBackground.ChangeSpriteVariant(Location);
		}
		else {

			Logger.Log("iActionGUI Not present", Category.UI);
		}
	}


	public void SetBackground(IActionGUI iActionGUI, Sprite sprite)
	{
		if (DicIActionGUI.ContainsKey(iActionGUI))
		{
			var _UIAction = DicIActionGUI[iActionGUI];
			_UIAction.IconBackground.SetSprite(sprite);
		}
		else {
			Logger.Log("iActionGUI Not present", Category.UI);
		}
	}

	public void AddAction(IActionGUI iActionGUI)
	{
		UIAction _UIAction;
		if (PooledUIAction.Count > 0)
		{
			_UIAction = PooledUIAction[0];
			PooledUIAction.RemoveAt(0);
		}
		else {
			_UIAction = Instantiate(UIAction);
			_UIAction.transform.SetParent(Panel.transform, false);
		}
		DicIActionGUI[iActionGUI] = _UIAction;
		_UIAction.SetUp(iActionGUI);

	}

	public void RemoveAction(IActionGUI iActionGUI)
	{
		if (DicIActionGUI.ContainsKey(iActionGUI))
		{
			var _UIAction = DicIActionGUI[iActionGUI];
			_UIAction.Pool();
			PooledUIAction.Add(_UIAction);
			DicIActionGUI.Remove(iActionGUI);
		}
		else {
			Logger.Log("iActionGUI Not present", Category.UI);
		}
	}
 
	public void OnRoundEnd()
	{
		foreach (var _Action in DicIActionGUI) { 
			_Action.Value.Pool();
		}
		DicIActionGUI = new Dictionary<IActionGUI, UIAction>();
	}
}
