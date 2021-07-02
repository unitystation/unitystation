using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TipsUI : MonoBehaviour
{
    [SerializeField] private ScriptableObjects.StringList GeneralTipsList;

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
        UI_Text.text = GeneralTipsList.Strings.PickRandom();
    }

    public void DisplayCustomTip(string tip)
    {
        UI_Text.text = tip;
    }
}
