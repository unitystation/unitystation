using System;
using System.Collections.Generic;
using Godot;

[Serializable]
public class CodeScanData
{
    public List<string> AllowedVerifierErrors { get; set; } = new List<string>();
    public List<string> WhitelistedNamespaces { get; set; } = new List<string>();
    public List<string> WhitelistedAssembliesDEBUG { get; set; } = new List<string>();
    public Dictionary<string, Dictionary<string, TypeData>> Types { get; set; } = new Dictionary<string, Dictionary<string, TypeData>>();
}

[Serializable]
public class TypeData
{
    public bool All { get; set; } = false;
    public string Inherit { get; set; } = "";
    public List<string> Methods { get; set; } = new List<string>();
    public List<string> Fields { get; set; } = new List<string>();
    public Dictionary<string, TypeData> NestedTypes { get; set; } = new Dictionary<string, TypeData>();
} 