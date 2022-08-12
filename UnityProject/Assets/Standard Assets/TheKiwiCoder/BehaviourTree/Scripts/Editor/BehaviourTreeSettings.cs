using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

// Create a new type of Settings Asset.
class BehaviourTreeSettings : ScriptableObject {
    public VisualTreeAsset behaviourTreeXml;
    public StyleSheet behaviourTreeStyle;
    public VisualTreeAsset nodeXml;
    public TextAsset scriptTemplateActionNode;
    public TextAsset scriptTemplateCompositeNode;
    public TextAsset scriptTemplateDecoratorNode;
    public string newNodeBasePath = "Assets/";

    static BehaviourTreeSettings FindSettings(){
        var guids = AssetDatabase.FindAssets("t:BehaviourTreeSettings");
        if (guids.Length > 1) {
            Debug.LogWarning($"Found multiple settings files, using the first.");
        }

        switch (guids.Length) {
            case 0:
                return null;
            default:
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<BehaviourTreeSettings>(path);
        }
    }

    internal static BehaviourTreeSettings GetOrCreateSettings() {
        var settings = FindSettings();
        if (settings == null) {
            settings = ScriptableObject.CreateInstance<BehaviourTreeSettings>();
            AssetDatabase.CreateAsset(settings, "Assets");
            AssetDatabase.SaveAssets();
        }
        return settings;
    }

    internal static SerializedObject GetSerializedSettings() {
        return new SerializedObject(GetOrCreateSettings());
    }
}

// Register a SettingsProvider using UIElements for the drawing framework:
static class MyCustomSettingsUIElementsRegister {
    [SettingsProvider]
    public static SettingsProvider CreateMyCustomSettingsProvider() {
        // First parameter is the path in the Settings window.
        // Second parameter is the scope of this setting: it only appears in the Settings window for the Project scope.
        var provider = new SettingsProvider("Project/MyCustomUIElementsSettings", SettingsScope.Project) {
            label = "BehaviourTree",
            // activateHandler is called when the user clicks on the Settings item in the Settings window.
            activateHandler = (searchContext, rootElement) => {
                var settings = BehaviourTreeSettings.GetSerializedSettings();

                // rootElement is a VisualElement. If you add any children to it, the OnGUI function
                // isn't called because the SettingsProvider uses the UIElements drawing framework.
                var title = new Label() {
                    text = "Behaviour Tree Settings"
                };
                title.AddToClassList("title");
                rootElement.Add(title);

                var properties = new VisualElement() {
                    style =
                    {
                        flexDirection = FlexDirection.Column
                    }
                };
                properties.AddToClassList("property-list");
                rootElement.Add(properties);

                properties.Add(new InspectorElement(settings));

                rootElement.Bind(settings);
            },
        };

        return provider;
    }
}
