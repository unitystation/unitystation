using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TransformExtensions
{

    public static void MoveToSection(this Transform transform, Section section, string subSectionName = "")
    {
        string sectionName = "No Section";

        if (section)
            sectionName = section.Name;

        string mapPath;
        if (string.IsNullOrEmpty(subSectionName))
            mapPath = sectionName + "/" + FindMapPath(transform);
        else
            mapPath = sectionName + "/" + subSectionName;
        var parentTransform = CreateMapPath(mapPath);

        transform.parent = parentTransform;
    }

    private static string FindMapPath(Transform transform)
    {

        if (transform.parent == null || transform.parent.parent != null && transform.parent.parent.tag == "Map")
        {
            return string.Empty;
        }

        return transform.parent.name + "/" + FindMapPath(transform.parent);
    }

    private static Transform CreateMapPath(string map)
    {
        var splitted = map.Split('/');

        var transform = GameObject.FindGameObjectWithTag("Map").transform;

        for (int i = 0; i < splitted.Length; i++)
        {
            var subTransform = transform.Find(splitted[i]);
            if (!subTransform)
            {
                subTransform = new GameObject(splitted[i]).transform;
                subTransform.parent = transform;
            }

            transform = subTransform;
        }

        return transform;
    }
}
