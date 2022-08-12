using System;
using System.Collections;
using System.IO;
using System.Xml;
using UnityEngine;


public class Lang_Bot
{
    private Hashtable strings;

    public Lang_Bot(string path, string language)
    {
        SetLanguage(path, language);
    }
    
    public void SetLanguage(string path, string language)
    {
        var xml = new XmlDocument();
        xml.Load(path);
     
        strings = new Hashtable();
        XmlElement element = xml.DocumentElement[language];
        if (element != null)
        {
            IEnumerator elemEnum = element.GetEnumerator();
            while (elemEnum.MoveNext())
            {
                XmlElement xmlItem = (XmlElement)elemEnum.Current;
                strings.Add(xmlItem.GetAttribute("name"), xmlItem.InnerText);
            }
        }
        else
        {
            Debug.LogWarning(language + " doesn't exist");
        }
    }

    public string GetString (string name) {
        if (!strings.ContainsKey(name)) {
            Debug.LogWarning(name + "doesn't exist");
            return "";
        }
 
        return (string)strings[name];
    }
}
