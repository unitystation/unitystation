using System;
using UnityEngine;

[Serializable]
[ExecuteInEditMode]
public class Section: ScriptableObject {
    [SerializeField]
    private new string name;
    public Color color;

    private Transform sectionObject;

    public string Name {
        get { return name; }
        set { name = value; UpdateSectionObject(); }
    }

    public void Init(string name, Color color) {
        this.name = name;
        this.color = color;

        FindSectionObject();
    }

    private void FindSectionObject() {
        if(string.IsNullOrEmpty(name))
            return;

        var map = GameObject.FindGameObjectWithTag("Map");

        if(map) {
            sectionObject = map.transform.Find(name);

            if(!sectionObject) {
                sectionObject = new GameObject(name).transform;
                sectionObject.parent = map.transform;
            }
        }
    }

    private void UpdateSectionObject() {
        if(!sectionObject)
            FindSectionObject();
		if(sectionObject != null)
        sectionObject.name = name;
    }
}