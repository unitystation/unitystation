using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using System.Linq;
using ModTool;

/// <summary>
/// Example mod manager. This menu displays all mods and lets you enable/disable them.
/// </summary>
public class ModMenu : MonoBehaviour
{    
    /// <summary>
    /// The content panel where the menu items will be parented
    /// </summary>
    public Transform menuContentPanel;

    /// <summary>
    /// The prefab for the mod menu item
    /// </summary>
    public ModItem modItemPrefab;

    /// <summary>
    /// Button that will start loading enabled mods
    /// </summary>
    public Button loadButton;

    /// <summary>
    /// Dictionary linking mod menu items with mods
    /// </summary>
    private Dictionary<Mod, ModItem> modItems;

    /// <summary>
    /// Are the enabled mods loaded?
    /// </summary>
    private bool isLoaded;
        
    void Start()
    {
        modItems = new Dictionary<Mod, ModItem>();

        //Subscribe to ModManager events for keeping track of found mods
        ModManager.ModFound += OnModFound;
        ModManager.ModRemoved += OnModRemoved;

        //Subscribe to ModManager events to keep track of loaded mods
        ModManager.ModLoaded += OnModLoaded;
        ModManager.ModUnloaded += OnModUnloaded;
        
        //Refresh and look mods in mod search directories
        ModManager.Refresh();

        //Start refreshing ModManager to look for changes every 2 seconds
        StartCoroutine(AutoRefresh(2));

        Application.runInBackground = true;        
    }
   
    private void OnModFound(Mod mod)
    {
        ModItem modItem = Instantiate(modItemPrefab, menuContentPanel);
        modItem.Initialize(mod);
        modItem.SetToggleInteractable(!isLoaded);
        modItems.Add(mod, modItem);
    }

    private void OnModRemoved(Mod mod)
    {
        ModItem modItem;

        if(modItems.TryGetValue(mod, out modItem))
        {
            modItems.Remove(mod);
            Destroy(modItem.gameObject);
        }
    }
        
    private void SetTogglesInteractable(bool interactable)
    {
        foreach (ModItem item in modItems.Values)
        {
            item.SetToggleInteractable(interactable);
        }
    }

    /// <summary>
    /// Toggle load or unload all enabled mods.
    /// </summary>
    public void LoadButton()
    {
        if (isLoaded)
        {
            Unload();
        }
        else
        {
            Load();
        }
    }

    IEnumerator AutoRefresh(float seconds)
    {
        while(true)
        {
            ModManager.Refresh();
            yield return new WaitForSeconds(seconds);
        }
    }

    private void Load()
    {
        //load mods
        foreach (Mod mod in modItems.Keys)
        {
            if(mod.isEnabled)
                mod.Load();
        }

        SetTogglesInteractable(false);

        loadButton.GetComponentInChildren<Text>().text = "U N L O A D";

        isLoaded = true;
    }

    private void Unload()
    {   
        //unload all mods - this will unload their scenes and destroy any associated objects
        foreach (Mod mod in modItems.Keys)
        {
            mod.Unload();
        }

        SetTogglesInteractable(true);

        loadButton.GetComponentInChildren<Text>().text = "L O A D";

        isLoaded = false;
    }
    
    private void OnModLoaded(Mod mod)
    {
        Debug.Log("Loaded Mod: " + mod.name);

        //load first scene (if present) when a mod is loaded
        ModScene scene = mod.scenes.FirstOrDefault();

        if (scene != null)
            scene.Load();       
    }

    private void OnModUnloaded(Mod mod)
    {
        Debug.Log("Unloaded Mod: " + mod.name);
    }
}