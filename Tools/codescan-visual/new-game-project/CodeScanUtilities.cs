using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public static class CodeScanUtilities
{
    /// <summary>
    /// Gets all unique method signatures across all types
    /// </summary>
    public static List<string> GetAllMethodSignatures(CodeScanData data)
    {
        var methods = new HashSet<string>();
        
        foreach (var namespaceKvp in data.Types)
        {
            foreach (var typeKvp in namespaceKvp.Value)
            {
                foreach (var method in typeKvp.Value.Methods)
                {
                    methods.Add(method);
                }
                
                // Check nested types
                foreach (var nestedKvp in typeKvp.Value.NestedTypes)
                {
                    foreach (var method in nestedKvp.Value.Methods)
                    {
                        methods.Add(method);
                    }
                }
            }
        }
        
        return methods.OrderBy(m => m).ToList();
    }
    
    /// <summary>
    /// Gets all unique field names across all types
    /// </summary>
    public static List<string> GetAllFieldNames(CodeScanData data)
    {
        var fields = new HashSet<string>();
        
        foreach (var namespaceKvp in data.Types)
        {
            foreach (var typeKvp in namespaceKvp.Value)
            {
                foreach (var field in typeKvp.Value.Fields)
                {
                    fields.Add(field);
                }
                
                // Check nested types
                foreach (var nestedKvp in typeKvp.Value.NestedTypes)
                {
                    foreach (var field in nestedKvp.Value.Fields)
                    {
                        fields.Add(field);
                    }
                }
            }
        }
        
        return fields.OrderBy(f => f).ToList();
    }
    
    /// <summary>
    /// Finds all types that have a specific inheritance policy
    /// </summary>
    public static List<(string Namespace, string TypeName)> GetTypesByInheritancePolicy(CodeScanData data, string policy)
    {
        var result = new List<(string, string)>();
        
        foreach (var namespaceKvp in data.Types)
        {
            foreach (var typeKvp in namespaceKvp.Value)
            {
                if (typeKvp.Value.Inherit == policy)
                {
                    result.Add((namespaceKvp.Key, typeKvp.Key));
                }
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Gets statistics about the CodeScan data
    /// </summary>
    public static CodeScanStatistics GetStatistics(CodeScanData data)
    {
        var stats = new CodeScanStatistics();
        
        stats.AllowedVerifierErrorsCount = data.AllowedVerifierErrors.Count;
        stats.WhitelistedNamespacesCount = data.WhitelistedNamespaces.Count;
        stats.WhitelistedAssembliesCount = data.WhitelistedAssembliesDEBUG.Count;
        stats.TotalNamespaces = data.Types.Count;
        
        foreach (var namespaceKvp in data.Types)
        {
            stats.TotalTypes += namespaceKvp.Value.Count;
            
            foreach (var typeKvp in namespaceKvp.Value)
            {
                var typeData = typeKvp.Value;
                stats.TotalMethods += typeData.Methods.Count;
                stats.TotalFields += typeData.Fields.Count;
                stats.TotalNestedTypes += typeData.NestedTypes.Count;
                
                if (typeData.All)
                    stats.TypesWithAllPolicy++;
                
                if (!string.IsNullOrEmpty(typeData.Inherit))
                    stats.TypesWithInheritancePolicy++;
            }
        }
        
        return stats;
    }
    
    /// <summary>
    /// Validates the CodeScan data for common issues
    /// </summary>
    public static List<string> ValidateCodeScanData(CodeScanData data)
    {
        var issues = new List<string>();
        
        // Check for empty namespaces
        foreach (var namespaceKvp in data.Types)
        {
            if (namespaceKvp.Value.Count == 0)
            {
                issues.Add($"Namespace '{namespaceKvp.Key}' has no types defined");
            }
        }
        
        // Check for types with no methods, fields, or nested types
        foreach (var namespaceKvp in data.Types)
        {
            foreach (var typeKvp in namespaceKvp.Value)
            {
                var typeData = typeKvp.Value;
                if (!typeData.All && 
                    typeData.Methods.Count == 0 && 
                    typeData.Fields.Count == 0 && 
                    typeData.NestedTypes.Count == 0)
                {
                    issues.Add($"Type '{namespaceKvp.Key}.{typeKvp.Key}' has no methods, fields, or nested types defined");
                }
            }
        }
        
        // Check for duplicate method signatures within the same type
        foreach (var namespaceKvp in data.Types)
        {
            foreach (var typeKvp in namespaceKvp.Value)
            {
                var typeData = typeKvp.Value;
                var duplicateMethods = typeData.Methods.GroupBy(m => m)
                                                     .Where(g => g.Count() > 1)
                                                     .Select(g => g.Key);
                
                foreach (var duplicate in duplicateMethods)
                {
                    issues.Add($"Type '{namespaceKvp.Key}.{typeKvp.Key}' has duplicate method: {duplicate}");
                }
            }
        }
        
        return issues;
    }
    
    /// <summary>
    /// Exports the CodeScan data to a formatted text report
    /// </summary>
    public static string ExportToTextReport(CodeScanData data, string title = "CodeScan Report")
    {
        var report = new System.Text.StringBuilder();
        var stats = GetStatistics(data);
        
        report.AppendLine(title);
        report.AppendLine(new string('=', title.Length));
        report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();
        
        // Summary
        report.AppendLine("SUMMARY");
        report.AppendLine("=======");
        report.AppendLine($"Namespaces: {stats.TotalNamespaces}");
        report.AppendLine($"Types: {stats.TotalTypes}");
        report.AppendLine($"Methods: {stats.TotalMethods}");
        report.AppendLine($"Fields: {stats.TotalFields}");
        report.AppendLine($"Nested Types: {stats.TotalNestedTypes}");
        report.AppendLine($"Types with 'All' policy: {stats.TypesWithAllPolicy}");
        report.AppendLine($"Types with inheritance policy: {stats.TypesWithInheritancePolicy}");
        report.AppendLine();
        
        // Allowed Verifier Errors
        if (data.AllowedVerifierErrors.Count > 0)
        {
            report.AppendLine("ALLOWED VERIFIER ERRORS");
            report.AppendLine("=======================");
            foreach (var error in data.AllowedVerifierErrors)
            {
                report.AppendLine($"- {error}");
            }
            report.AppendLine();
        }
        
        // Whitelisted Namespaces
        if (data.WhitelistedNamespaces.Count > 0)
        {
            report.AppendLine("WHITELISTED NAMESPACES");
            report.AppendLine("=====================");
            foreach (var ns in data.WhitelistedNamespaces)
            {
                report.AppendLine($"- {ns}");
            }
            report.AppendLine();
        }
        
        // Whitelisted Assemblies
        if (data.WhitelistedAssembliesDEBUG.Count > 0)
        {
            report.AppendLine("WHITELISTED ASSEMBLIES (DEBUG)");
            report.AppendLine("=============================");
            foreach (var assembly in data.WhitelistedAssembliesDEBUG)
            {
                report.AppendLine($"- {assembly}");
            }
            report.AppendLine();
        }
        
        // Detailed breakdown by namespace
        report.AppendLine("DETAILED BREAKDOWN");
        report.AppendLine("==================");
        
        foreach (var namespaceKvp in data.Types.OrderBy(kvp => kvp.Key))
        {
            report.AppendLine($"\nNamespace: {namespaceKvp.Key}");
            report.AppendLine($"  Types: {namespaceKvp.Value.Count}");
            
            foreach (var typeKvp in namespaceKvp.Value.OrderBy(kvp => kvp.Key))
            {
                var typeData = typeKvp.Value;
                report.AppendLine($"    - {typeKvp.Key}");
                
                if (typeData.All)
                    report.AppendLine("      All: true");
                if (!string.IsNullOrEmpty(typeData.Inherit))
                    report.AppendLine($"      Inherit: {typeData.Inherit}");
                if (typeData.Methods.Count > 0)
                    report.AppendLine($"      Methods: {typeData.Methods.Count}");
                if (typeData.Fields.Count > 0)
                    report.AppendLine($"      Fields: {typeData.Fields.Count}");
                if (typeData.NestedTypes.Count > 0)
                    report.AppendLine($"      Nested Types: {typeData.NestedTypes.Count}");
            }
        }
        
        return report.ToString();
    }
}

/// <summary>
/// Statistics about CodeScan data
/// </summary>
public class CodeScanStatistics
{
    public int AllowedVerifierErrorsCount { get; set; }
    public int WhitelistedNamespacesCount { get; set; }
    public int WhitelistedAssembliesCount { get; set; }
    public int TotalNamespaces { get; set; }
    public int TotalTypes { get; set; }
    public int TotalMethods { get; set; }
    public int TotalFields { get; set; }
    public int TotalNestedTypes { get; set; }
    public int TypesWithAllPolicy { get; set; }
    public int TypesWithInheritancePolicy { get; set; }
} 