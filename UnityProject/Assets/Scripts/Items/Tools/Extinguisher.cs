using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Extinguisher : MonoBehaviour
{
    public bool isOn = false;
    public Sprite sprClosed;
    public Sprite sprOpen;

    //Used to open and close fire extinguisher
    public void ToggleExtinguisher(GameObject originator)
    {
        isOn = !isOn;
        SpriteRenderer spr = GetComponentInChildren<SpriteRenderer>();
        if(isOn)
        {
            UIManager.Instance.hands.CurrentSlot.image.sprite = sprOpen;
            spr.sprite = sprOpen;
        }
        else
        {
            UIManager.Instance.hands.CurrentSlot.image.sprite = sprClosed;
            spr.sprite = sprClosed;
        }
    }
}
