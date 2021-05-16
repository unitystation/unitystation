using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TipsUI : MonoBehaviour
{
    [SerializeField] private TipsSO SO;

    [SerializeField] private TMP_Text UI_Text;

    [SerializeField] private bool ShowTipOnStartup = true;

    private void Awake() {
        if(ShowTipOnStartup)
        {
            DisplayRandomTip();
        }
        else
        {
            UI_Text.text = "";
        }
    }

    public void DisplayRandomTip()
    {
        System.Random randomValue = new System.Random();
        string TipToDisplay = SO.Tips[randomValue.Next(SO.Tips.Count)];

        UI_Text.text = "Tip : " + TipToDisplay;
    }

    public void DisplayCustomTip(string tip)
    {
        UI_Text.text = tip;
    }
}