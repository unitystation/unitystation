using UnityEngine;
using UnityEngine.UI;

public class PanelHudBottomController : MonoBehaviour
{
    public UI_ItemSlot backpackItemSlot;
    public UI_ItemSlot PDAItemSlot;
    public UI_ItemSlot beltItemSlot;

    [SerializeField] private Text backpackKeybindText;
    [SerializeField] private Text PDAKeybindText;
    [SerializeField] private Text beltKeybindText;

    public void SetBackPackKeybindText(KeyCode key)
    {
        backpackKeybindText.text = key.ToString();
    }

    public void SetPDAKeybindText(KeyCode key)
    {
        PDAKeybindText.text = key.ToString();
    }

    public void SetBeltKeybindText(KeyCode key)
    {
        beltKeybindText.text = key.ToString();
    }
}
