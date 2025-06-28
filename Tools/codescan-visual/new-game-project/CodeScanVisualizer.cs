using System;
using System.Collections.Generic;
using Godot;

public partial class CodeScanVisualizer : Control
{
    [Export] public string JsonFilePath = "res://CodeScanList.json";
    
    private CodeScanData _codeScanData;
    private Tree _tree;
    private LineEdit _searchBox;
    private Label _statusLabel;
    private Button _refreshButton;
    private Button _expandAllButton;
    private Button _collapseAllButton;
    
    public override void _Ready()
    {
        SetupUI();
        LoadCodeScanData();
    }
    
    private void SetupUI()
    {
        // Create main VBoxContainer
        var mainContainer = new VBoxContainer();
        mainContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(mainContainer);
        
        // Create top toolbar
        var toolbar = new HBoxContainer();
        mainContainer.AddChild(toolbar);
        
        // Search box
        _searchBox = new LineEdit();
        _searchBox.PlaceholderText = "Search namespaces, types, methods...";
        _searchBox.TextChanged += OnSearchTextChanged;
        toolbar.AddChild(_searchBox);
        
        // Refresh button
        _refreshButton = new Button();
        _refreshButton.Text = "Refresh";
        _refreshButton.Pressed += OnRefreshPressed;
        toolbar.AddChild(_refreshButton);
        
        // Expand all button
        _expandAllButton = new Button();
        _expandAllButton.Text = "Expand All";
        _expandAllButton.Pressed += OnExpandAllPressed;
        toolbar.AddChild(_expandAllButton);
        
        // Collapse all button
        _collapseAllButton = new Button();
        _collapseAllButton.Text = "Collapse All";
        _collapseAllButton.Pressed += OnCollapseAllPressed;
        toolbar.AddChild(_collapseAllButton);
        
        // Status label
        _statusLabel = new Label();
        _statusLabel.Text = "Ready";
        mainContainer.AddChild(_statusLabel);
        
        // Create tree
        _tree = new Tree();
        _tree.SetAnchorsPreset(LayoutPreset.FullRect);
        _tree.Columns = 3;
        _tree.SetColumnTitle(0, "Name");
        _tree.SetColumnTitle(1, "Type");
        _tree.SetColumnTitle(2, "Details");
        _tree.SetColumnExpand(0, true);
        _tree.SetColumnExpand(1, false);
        _tree.SetColumnExpand(2, true);
        _tree.SetColumnClipContent(0, true);
        _tree.SetColumnClipContent(1, true);
        _tree.SetColumnClipContent(2, true);
        mainContainer.AddChild(_tree);
    }
    
    private void LoadCodeScanData()
    {
        _statusLabel.Text = "Loading CodeScan data...";
        
        _codeScanData = CodeScanParser.LoadFromFile(JsonFilePath);
        
        if (_codeScanData.Types.Count > 0)
        {
            PopulateTree();
            _statusLabel.Text = $"Loaded {_codeScanData.Types.Count} namespaces";
        }
        else
        {
            _statusLabel.Text = "No data loaded or file not found";
        }
    }
    
    private void PopulateTree()
    {
        _tree.Clear();
        var root = _tree.CreateItem();
        
        // Add allowed verifier errors
        if (_codeScanData.AllowedVerifierErrors.Count > 0)
        {
            var verifierItem = _tree.CreateItem(root);
            verifierItem.SetText(0, "Allowed Verifier Errors");
            verifierItem.SetText(1, "List");
            verifierItem.SetText(2, $"{_codeScanData.AllowedVerifierErrors.Count} items");
            
            foreach (var error in _codeScanData.AllowedVerifierErrors)
            {
                var errorItem = _tree.CreateItem(verifierItem);
                errorItem.SetText(0, error);
                errorItem.SetText(1, "Error");
            }
        }
        
        // Add whitelisted namespaces
        if (_codeScanData.WhitelistedNamespaces.Count > 0)
        {
            var whitelistItem = _tree.CreateItem(root);
            whitelistItem.SetText(0, "Whitelisted Namespaces");
            whitelistItem.SetText(1, "List");
            whitelistItem.SetText(2, $"{_codeScanData.WhitelistedNamespaces.Count} items");
            
            foreach (var ns in _codeScanData.WhitelistedNamespaces)
            {
                var nsItem = _tree.CreateItem(whitelistItem);
                nsItem.SetText(0, ns);
                nsItem.SetText(1, "Namespace");
            }
        }
        
        // Add whitelisted assemblies
        if (_codeScanData.WhitelistedAssembliesDEBUG.Count > 0)
        {
            var assemblyItem = _tree.CreateItem(root);
            assemblyItem.SetText(0, "Whitelisted Assemblies (DEBUG)");
            assemblyItem.SetText(1, "List");
            assemblyItem.SetText(2, $"{_codeScanData.WhitelistedAssembliesDEBUG.Count} items");
            
            foreach (var assembly in _codeScanData.WhitelistedAssembliesDEBUG)
            {
                var assemblyListItem = _tree.CreateItem(assemblyItem);
                assemblyListItem.SetText(0, assembly);
                assemblyListItem.SetText(1, "Assembly");
            }
        }
        
        // Add types by namespace
        foreach (var namespaceKvp in _codeScanData.Types)
        {
            var namespaceItem = _tree.CreateItem(root);
            namespaceItem.SetText(0, namespaceKvp.Key);
            namespaceItem.SetText(1, "Namespace");
            namespaceItem.SetText(2, $"{namespaceKvp.Value.Count} types");
            
            foreach (var typeKvp in namespaceKvp.Value)
            {
                var typeItem = _tree.CreateItem(namespaceItem);
                typeItem.SetText(0, typeKvp.Key);
                typeItem.SetText(1, "Type");
                
                var typeData = typeKvp.Value;
                var details = new List<string>();
                
                if (typeData.All)
                    details.Add("All: true");
                if (!string.IsNullOrEmpty(typeData.Inherit))
                    details.Add($"Inherit: {typeData.Inherit}");
                if (typeData.Methods.Count > 0)
                    details.Add($"{typeData.Methods.Count} methods");
                if (typeData.Fields.Count > 0)
                    details.Add($"{typeData.Fields.Count} fields");
                if (typeData.NestedTypes.Count > 0)
                    details.Add($"{typeData.NestedTypes.Count} nested types");
                
                typeItem.SetText(2, string.Join(", ", details));
                
                // Add methods
                if (typeData.Methods.Count > 0)
                {
                    var methodsItem = _tree.CreateItem(typeItem);
                    methodsItem.SetText(0, "Methods");
                    methodsItem.SetText(1, "List");
                    methodsItem.SetText(2, $"{typeData.Methods.Count} methods");
                    
                    foreach (var method in typeData.Methods)
                    {
                        var methodItem = _tree.CreateItem(methodsItem);
                        methodItem.SetText(0, method);
                        methodItem.SetText(1, "Method");
                    }
                }
                
                // Add fields
                if (typeData.Fields.Count > 0)
                {
                    var fieldsItem = _tree.CreateItem(typeItem);
                    fieldsItem.SetText(0, "Fields");
                    fieldsItem.SetText(1, "List");
                    fieldsItem.SetText(2, $"{typeData.Fields.Count} fields");
                    
                    foreach (var field in typeData.Fields)
                    {
                        var fieldItem = _tree.CreateItem(fieldsItem);
                        fieldItem.SetText(0, field);
                        fieldItem.SetText(1, "Field");
                    }
                }
                
                // Add nested types
                if (typeData.NestedTypes.Count > 0)
                {
                    var nestedItem = _tree.CreateItem(typeItem);
                    nestedItem.SetText(0, "Nested Types");
                    nestedItem.SetText(1, "List");
                    nestedItem.SetText(2, $"{typeData.NestedTypes.Count} nested types");
                    
                    foreach (var nestedKvp in typeData.NestedTypes)
                    {
                        var nestedTypeItem = _tree.CreateItem(nestedItem);
                        nestedTypeItem.SetText(0, nestedKvp.Key);
                        nestedTypeItem.SetText(1, "Nested Type");
                        
                        var nestedTypeData = nestedKvp.Value;
                        var nestedDetails = new List<string>();
                        
                        if (nestedTypeData.All)
                            nestedDetails.Add("All: true");
                        if (!string.IsNullOrEmpty(nestedTypeData.Inherit))
                            nestedDetails.Add($"Inherit: {nestedTypeData.Inherit}");
                        if (nestedTypeData.Methods.Count > 0)
                            nestedDetails.Add($"{nestedTypeData.Methods.Count} methods");
                        if (nestedTypeData.Fields.Count > 0)
                            nestedDetails.Add($"{nestedTypeData.Fields.Count} fields");
                        
                        nestedTypeItem.SetText(2, string.Join(", ", nestedDetails));
                    }
                }
            }
        }
    }
    
    private void OnSearchTextChanged(string newText)
    {
        if (string.IsNullOrEmpty(newText))
        {
            // Show all items
            ShowAllItems(_tree.GetRoot());
        }
        else
        {
            // Filter items
            FilterItems(_tree.GetRoot(), newText.ToLower());
        }
    }
    
    private void ShowAllItems(TreeItem item)
    {
        if (item == null) return;
        
        item.Visible = true;
        var child = item.GetFirstChild();
        while (child != null)
        {
            ShowAllItems(child);
            child = child.GetNext();
        }
    }
    
    private bool FilterItems(TreeItem item, string searchText)
    {
        if (item == null) return false;
        
        bool hasVisibleChild = false;
        var child = item.GetFirstChild();
        
        while (child != null)
        {
            if (FilterItems(child, searchText))
            {
                hasVisibleChild = true;
            }
            child = child.GetNext();
        }
        
        bool matches = item.GetText(0).ToLower().Contains(searchText) ||
                      item.GetText(1).ToLower().Contains(searchText) ||
                      item.GetText(2).ToLower().Contains(searchText);
        
        item.Visible = matches || hasVisibleChild;
        return matches || hasVisibleChild;
    }
    
    private void OnRefreshPressed()
    {
        LoadCodeScanData();
    }
    
    private void OnExpandAllPressed()
    {
        ExpandAllItems(_tree.GetRoot());
    }
    
    private void OnCollapseAllPressed()
    {
        CollapseAllItems(_tree.GetRoot());
    }
    
    private void ExpandAllItems(TreeItem item)
    {
        if (item == null) return;
        
        item.Collapsed = false;
        var child = item.GetFirstChild();
        while (child != null)
        {
            ExpandAllItems(child);
            child = child.GetNext();
        }
    }
    
    private void CollapseAllItems(TreeItem item)
    {
        if (item == null) return;
        
        item.Collapsed = true;
        var child = item.GetFirstChild();
        while (child != null)
        {
            CollapseAllItems(child);
            child = child.GetNext();
        }
    }
} 