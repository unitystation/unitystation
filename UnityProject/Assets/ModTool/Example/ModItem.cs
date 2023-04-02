using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using ModTool;
using Debug = System.Diagnostics.Debug;

public class ModItem : MonoBehaviour {

    public Text modName;
    public Text modType;

    public Toggle toggle;

    public Mod mod;

    /// <summary>
    /// Initialze this ModItem with a Mod and ModMenu.
    /// </summary>
    /// <param name="mod"></param>
    /// <param name="modMenu"></param>
	public void Initialize(Mod mod)
    {
	    var Stopwatch = new Stopwatch();
	    Stopwatch.Start();

        this.mod = mod;

        modName.text = mod.name;
        modType.text = mod.contentType.ToString();

        toggle.isOn = mod.isEnabled;

        toggle.onValueChanged.AddListener( value => Toggle(value));

        Stopwatch.Stop();


    }

    /// <summary>
    /// Toggle whether the mod should be loaded
    /// </summary>
    public void Toggle(bool isEnabled)
    {
        mod.isEnabled = isEnabled;
	}

    /// <summary>
    /// Enable or disable this ModItem's toggle.
    /// </summary>
    /// <param name="interactable"></param>
    public void SetToggleInteractable(bool interactable)
    {
        toggle.interactable = interactable;
    }
}
