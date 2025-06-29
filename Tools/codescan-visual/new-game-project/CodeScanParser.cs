using System;
using System.Collections.Generic;
using Godot;
using System.Text.Json;

public class CodeScanParser
{
    public static CodeScanData ParseCodeScanList(string jsonContent)
    {
        try
        {
            // Use System.Text.Json for parsing
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var data = JsonSerializer.Deserialize<CodeScanData>(jsonContent, options);
            return data ?? new CodeScanData();
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error parsing CodeScanList.json: {e.Message}");
            return new CodeScanData();
        }
    }
    
    public static CodeScanData LoadFromFile(string filePath)
    {
        try
        {
            if (!FileAccess.FileExists(filePath))
            {
                GD.PrintErr($"File not found: {filePath}");
                return new CodeScanData();
            }
            
            var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
            var content = file.GetAsText();
            file.Close();
            
            return ParseCodeScanList(content);
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error loading file {filePath}: {e.Message}");
            return new CodeScanData();
        }
    }
    
    public static List<string> GetAllNamespaces(CodeScanData data)
    {
        return new List<string>(data.Types.Keys);
    }
    
    public static List<string> GetTypesInNamespace(CodeScanData data, string namespaceName)
    {
        var types = new List<string>();
        if (data.Types.ContainsKey(namespaceName))
        {
            foreach (var typeName in data.Types[namespaceName].Keys)
            {
                types.Add(typeName);
            }
        }
        return types;
    }
    
    public static TypeData GetTypeData(CodeScanData data, string namespaceName, string typeName)
    {
        if (data.Types.ContainsKey(namespaceName) &&
            data.Types[namespaceName].ContainsKey(typeName))
        {
            return data.Types[namespaceName][typeName];
        }
        return null;
    }
} 