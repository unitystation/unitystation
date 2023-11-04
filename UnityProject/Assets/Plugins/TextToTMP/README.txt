= Text to TextMesh Pro Upgrade Tool (v1.1.1) =

Online documentation available at: https://github.com/yasirkula/UnityTextToTextMeshProUpgradeTool
E-mail: yasirkula@gmail.com


1. ABOUT
This asset helps you upgrade the Text, InputField, Dropdown and TextMesh objects in your projects to their TextMesh Pro variants. It also upgrades the scripts so that e.g. Text variables in those scripts become TMP_Text variables. Then, it reconnects the references to the upgraded components (e.g. if a public variable was referencing an upgraded Text component, it will now reference the corresponding TextMeshProUGUI component).


2. HOW TO
Before proceeding, you are strongly recommended to backup your project; just in case.

- Open the "Window-Upgrade Text to TMP" window
- Add the prefabs, Scenes, scripts and ScriptableObjects to upgrade to the "Assets & Scenes To Upgrade" list (if you add a folder there, its whole contents will be upgraded)(ScriptableObjects' Font variables will be upgraded). If an Object wasn't added to that list but it had references to the upgraded components, those references will be lost
- To determine which Unity Fonts will be upgraded to which TextMesh Pro FontAssets, use the "Font Upgrades" list
- Hit START and then follow the presented instructions. To summarize:
  - "Step 1/3: Upgrading Scripts": Decide which scripts should be upgraded; e.g. Text variables in those scripts will become TMP_Text variables
  - "Step 2/3: Upgrading Components": Decide which prefabs/Scenes should be upgraded; e.g. Text components in those prefabs/Scenes will be upgraded to TextMeshProUGUI components
  - "Step 3/3: Reconnecting References": Decide whether or not the references to the upgraded components should be restored; e.g. if a public variable was referencing an upgraded Text component, it will now reference the corresponding TextMeshProUGUI component


3. KNOWN LIMITATIONS
- If an InputField or Dropdown prefab instance's UnityEvent is modified (i.e. it's different from the prefab asset), that modification can't be restored after the upgrade process on Unity 2019.2 or earlier if a script was modified during the upgrade process


4. EXAMPLES
Please see: https://github.com/yasirkula/UnityTextToTextMeshProUpgradeTool#examples