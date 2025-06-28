using System;
using System.Collections.Generic;
using Godot;

public partial class CodeScanVisualizerEnhanced : Control
{
    [Export] public string JsonFilePath = "res://CodeScanList.json";

    private CodeScanData _codeScanData;
    private Tree _tree;
    private LineEdit _searchBox;
    private Label _statusLabel;
    private Label _statsLabel;
    private Button _refreshButton;
    private Button _expandAllButton;
    private Button _collapseAllButton;
    private Button _exportButton;
    private Button _addItemButton;
    private Button _exportJsonButton;
    private TabContainer _tabContainer;
    private RichTextLabel _jsonViewer;
    private AcceptDialog _addItemDialog;
    private OptionButton _addItemTypeOption;
    private OptionButton _addItemNamespaceOption;
    private OptionButton _addItemTypeParentOption;
    private Label _addItemExtraLabel;
    private LineEdit _addItemNameEdit;
    private Button _addItemConfirmButton;
    private CheckBox _addTypeAllCheckbox;
    private string _pendingAddNamespace = null;
    private string _pendingAddType = null;
    private bool _pendingAddTypePrompt = false;

    public override void _Ready()
    {
        SetupUI();
        LoadCodeScanData();
        _addItemDialog.Connect("popup_hide", new Callable(this, nameof(OnAddItemDialogHide)));
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
        _searchBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        toolbar.AddChild(_searchBox);

        // Add Item button
        _addItemButton = new Button();
        _addItemButton.Text = "➕ Add Item";
        _addItemButton.Pressed += OnAddItemPressed;
        toolbar.AddChild(_addItemButton);

        // Refresh button
        _refreshButton = new Button();
        _refreshButton.Text = "🔄 Refresh";
        _refreshButton.Pressed += OnRefreshPressed;
        toolbar.AddChild(_refreshButton);

        // Expand all button
        _expandAllButton = new Button();
        _expandAllButton.Text = "📂 Expand All";
        _expandAllButton.Pressed += OnExpandAllPressed;
        toolbar.AddChild(_expandAllButton);

        // Collapse all button
        _collapseAllButton = new Button();
        _collapseAllButton.Text = "📁 Collapse All";
        _collapseAllButton.Pressed += OnCollapseAllPressed;
        toolbar.AddChild(_collapseAllButton);

        // Export Stats button
        _exportButton = new Button();
        _exportButton.Text = "💾 Export Stats";
        _exportButton.Pressed += OnExportPressed;
        toolbar.AddChild(_exportButton);

        // Export JSON button
        _exportJsonButton = new Button();
        _exportJsonButton.Text = "📝 Export JSON";
        _exportJsonButton.Pressed += OnExportJsonPressed;
        toolbar.AddChild(_exportJsonButton);

        // Status and stats labels
        var infoContainer = new HBoxContainer();
        mainContainer.AddChild(infoContainer);

        _statusLabel = new Label();
        _statusLabel.Text = "Ready";
        infoContainer.AddChild(_statusLabel);

        _statsLabel = new Label();
        _statsLabel.Text = "";
        _statsLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _statsLabel.HorizontalAlignment = HorizontalAlignment.Right;
        infoContainer.AddChild(_statsLabel);

        // Create tab container
        _tabContainer = new TabContainer();
        _tabContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _tabContainer.CustomMinimumSize = new Vector2(400, 580);
        mainContainer.AddChild(_tabContainer);

        // Tree view tab
        var treeTab = new Control();
        treeTab.Name = "Tree View";
        _tabContainer.AddChild(treeTab);

        // Create tree
        _tree = new Tree();
        _tree.SetAnchorsPreset(Control.LayoutPreset.FullRect);
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
        treeTab.AddChild(_tree);

        // JSON viewer tab
        var jsonTab = new Control();
        jsonTab.Name = "JSON Viewer";
        _tabContainer.AddChild(jsonTab);

        _jsonViewer = new RichTextLabel();
        _jsonViewer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _jsonViewer.BbcodeEnabled = true;
        _jsonViewer.ScrollFollowing = true;
        jsonTab.AddChild(_jsonViewer);

        // Add Item Dialog
        _addItemDialog = new AcceptDialog();
        _addItemDialog.Title = "Add New Item";
        _addItemDialog.DialogText = "Choose what you want to add and enter the name:";
        _addItemDialog.Exclusive = true;
        AddChild(_addItemDialog);

        var vbox = new VBoxContainer();
        _addItemDialog.AddChild(vbox);

        _addItemTypeOption = new OptionButton();
        _addItemTypeOption.AddItem("📦 Namespace");
        _addItemTypeOption.AddItem("🔷 Type");
        _addItemTypeOption.AddItem("⚡ Method");
        _addItemTypeOption.AddItem("📝 Field");
        _addItemTypeOption.ItemSelected += OnAddItemTypeChanged;
        vbox.AddChild(_addItemTypeOption);

        _addItemNamespaceOption = new OptionButton();
        vbox.AddChild(_addItemNamespaceOption);
        _addItemNamespaceOption.Visible = false;

        _addItemTypeParentOption = new OptionButton();
        vbox.AddChild(_addItemTypeParentOption);
        _addItemTypeParentOption.Visible = false;

        _addTypeAllCheckbox = new CheckBox();
        _addTypeAllCheckbox.Text = "All: true (allow all members)";
        vbox.AddChild(_addTypeAllCheckbox);
        _addTypeAllCheckbox.Visible = false;

        _addItemExtraLabel = new Label();
        vbox.AddChild(_addItemExtraLabel);
        _addItemExtraLabel.Visible = false;

        _addItemNameEdit = new LineEdit();
        _addItemNameEdit.PlaceholderText = "Enter name...";
        vbox.AddChild(_addItemNameEdit);

        _addItemConfirmButton = new Button();
        _addItemConfirmButton.Text = "Add";
        _addItemConfirmButton.Pressed += OnAddItemConfirmPressed;
        vbox.AddChild(_addItemConfirmButton);
    }

    private void LoadCodeScanData()
    {
        _statusLabel.Text = "Loading CodeScan data...";

        _codeScanData = CodeScanParser.LoadFromFile(JsonFilePath);

        if (_codeScanData.Types.Count > 0)
        {
            PopulateTree();
            UpdateStatistics();
            LoadJsonViewer();
            _statusLabel.Text = $"Loaded {_codeScanData.Types.Count} namespaces";
        }
        else
        {
            _statusLabel.Text = "No data loaded or file not found";
            _statsLabel.Text = "";
        }
    }

    private void UpdateStatistics()
    {
        if (_codeScanData == null) return;

        int totalTypes = 0;
        int totalMethods = 0;
        int totalFields = 0;
        int totalNestedTypes = 0;

        foreach (var namespaceKvp in _codeScanData.Types)
        {
            foreach (var typeKvp in namespaceKvp.Value)
            {
                totalTypes++;
                var typeData = typeKvp.Value;
                totalMethods += typeData.Methods.Count;
                totalFields += typeData.Fields.Count;
                totalNestedTypes += typeData.NestedTypes.Count;
            }
        }

        _statsLabel.Text = $"Namespaces: {_codeScanData.Types.Count} | Types: {totalTypes} | Methods: {totalMethods} | Fields: {totalFields} | Nested: {totalNestedTypes}";
    }

    private void LoadJsonViewer()
    {
        try
        {
            if (FileAccess.FileExists(JsonFilePath))
            {
                var file = FileAccess.Open(JsonFilePath, FileAccess.ModeFlags.Read);
                var content = file.GetAsText();
                file.Close();

                // Format JSON for display
                var formattedJson = FormatJsonForDisplay(content);
                _jsonViewer.Text = formattedJson;
            }
        }
        catch (Exception e)
        {
            _jsonViewer.Text = $"Error loading JSON: {e.Message}";
        }
    }

    private string FormatJsonForDisplay(string json)
    {
        try
        {
            // Simple JSON formatting for display
            var formatted = json.Replace("{", "{\n  ")
                               .Replace("}", "\n}")
                               .Replace(",", ",\n  ")
                               .Replace(",\n  \n}", "\n}");

            return $"[code]{formatted}[/code]";
        }
        catch
        {
            return json;
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
            verifierItem.SetText(0, "🔴 Allowed Verifier Errors");
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
            whitelistItem.SetText(0, "✅ Whitelisted Namespaces");
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
            assemblyItem.SetText(0, "🔧 Whitelisted Assemblies (DEBUG)");
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
            namespaceItem.SetText(0, $"📦 {namespaceKvp.Key}");
            namespaceItem.SetText(1, "Namespace");
            namespaceItem.SetText(2, $"{namespaceKvp.Value.Count} types");

            foreach (var typeKvp in namespaceKvp.Value)
            {
                var typeItem = _tree.CreateItem(namespaceItem);
                typeItem.SetText(0, $"🔷 {typeKvp.Key}");
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
                    methodsItem.SetText(0, "⚡ Methods");
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
                    fieldsItem.SetText(0, "📝 Fields");
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
                    nestedItem.SetText(0, "🔶 Nested Types");
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
            ShowAllItems(_tree.GetRoot());
        }
        else
        {
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

    private void OnExportPressed()
    {
        ExportStatistics();
    }

    // Utility to replace unicode escapes for <, >, `
    private string DeUnicodeGenericMarkers(string json)
    {
        return json
            .Replace("\\u003C", "<")
            .Replace("\\u003E", ">")
            .Replace("\\u0060", "`");
    }

    private void OnExportJsonPressed()
    {
        try
        {
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            var json = System.Text.Json.JsonSerializer.Serialize(_codeScanData, options);
            json = DeUnicodeGenericMarkers(json);
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var filePath = $"res://CodeScanList-{timestamp}.json";
            var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
            file.StoreString(json);
            file.Close();
            _statusLabel.Text = $"Exported JSON to: {filePath}";
        }
        catch (Exception e)
        {
            _statusLabel.Text = $"Export failed: {e.Message}";
        }
    }

    private void ExportStatistics()
    {
        if (_codeScanData == null) return;

        try
        {
            var report = GenerateStatisticsReport();
            var filePath = "user://codescan_statistics.txt";

            var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
            file.StoreString(report);
            file.Close();

            _statusLabel.Text = $"Statistics exported to: {filePath}";
        }
        catch (Exception e)
        {
            _statusLabel.Text = $"Export failed: {e.Message}";
        }
    }

    private string GenerateStatisticsReport()
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("CodeScan Statistics Report");
        report.AppendLine("=========================");
        report.AppendLine($"Generated: {DateTime.Now}");
        report.AppendLine();

        report.AppendLine($"Allowed Verifier Errors: {_codeScanData.AllowedVerifierErrors.Count}");
        report.AppendLine($"Whitelisted Namespaces: {_codeScanData.WhitelistedNamespaces.Count}");
        report.AppendLine($"Whitelisted Assemblies (DEBUG): {_codeScanData.WhitelistedAssembliesDEBUG.Count}");
        report.AppendLine();

        int totalTypes = 0;
        int totalMethods = 0;
        int totalFields = 0;
        int totalNestedTypes = 0;

        report.AppendLine("Namespaces and Types:");
        report.AppendLine("====================");

        foreach (var namespaceKvp in _codeScanData.Types)
        {
            report.AppendLine($"\nNamespace: {namespaceKvp.Key}");
            report.AppendLine($"  Types: {namespaceKvp.Value.Count}");

            foreach (var typeKvp in namespaceKvp.Value)
            {
                totalTypes++;
                var typeData = typeKvp.Value;
                totalMethods += typeData.Methods.Count;
                totalFields += typeData.Fields.Count;
                totalNestedTypes += typeData.NestedTypes.Count;

                report.AppendLine($"    - {typeKvp.Key}");
                if (typeData.All) report.AppendLine("      All: true");
                if (!string.IsNullOrEmpty(typeData.Inherit)) report.AppendLine($"      Inherit: {typeData.Inherit}");
                if (typeData.Methods.Count > 0) report.AppendLine($"      Methods: {typeData.Methods.Count}");
                if (typeData.Fields.Count > 0) report.AppendLine($"      Fields: {typeData.Fields.Count}");
                if (typeData.NestedTypes.Count > 0) report.AppendLine($"      Nested Types: {typeData.NestedTypes.Count}");
            }
        }

        report.AppendLine();
        report.AppendLine("Summary:");
        report.AppendLine("========");
        report.AppendLine($"Total Namespaces: {_codeScanData.Types.Count}");
        report.AppendLine($"Total Types: {totalTypes}");
        report.AppendLine($"Total Methods: {totalMethods}");
        report.AppendLine($"Total Fields: {totalFields}");
        report.AppendLine($"Total Nested Types: {totalNestedTypes}");

        return report.ToString();
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

    private void OnAddItemPressed()
    {
        _addItemNameEdit.Text = "";
        _addItemTypeOption.Selected = 0;
        UpdateAddItemDialogUI();
        _addItemDialog.PopupCentered();
    }

    private void OnAddItemTypeChanged(long idx)
    {
        UpdateAddItemDialogUI();
    }

    private void UpdateAddItemDialogUI()
    {
        var selected = _addItemTypeOption.Selected;
        _addItemNamespaceOption.Visible = false;
        _addItemTypeParentOption.Visible = false;
        _addItemExtraLabel.Visible = false;
        _addTypeAllCheckbox.Visible = false;
        _addItemNamespaceOption.Clear();
        _addItemTypeParentOption.Clear();
        if (selected == 0) // Namespace
        {
            _addItemNameEdit.PlaceholderText = "Enter namespace name...";
        }
        else if (selected == 1) // Type
        {
            _addItemNamespaceOption.Visible = true;
            _addTypeAllCheckbox.Visible = true;
            _addTypeAllCheckbox.ButtonPressed = false;
            _addItemNameEdit.PlaceholderText = "Enter type name...";
            foreach (var ns in _codeScanData.Types.Keys)
                _addItemNamespaceOption.AddItem(ns);
            if (_addItemNamespaceOption.ItemCount == 0)
                _addItemNamespaceOption.AddItem("<No namespaces>");
        }
        else if (selected == 2 || selected == 3) // Method or Field
        {
            _addItemNamespaceOption.Visible = true;
            _addItemTypeParentOption.Visible = true;
            _addItemNameEdit.PlaceholderText = selected == 2 ? "Enter method signature..." : "Enter field name...";
            foreach (var ns in _codeScanData.Types.Keys)
                _addItemNamespaceOption.AddItem(ns);
            if (_addItemNamespaceOption.ItemCount == 0)
                _addItemNamespaceOption.AddItem("<No namespaces>");
            UpdateTypeParentOptions();
        }
    }

    private void UpdateTypeParentOptions()
    {
        _addItemTypeParentOption.Clear();
        var nsIdx = _addItemNamespaceOption.Selected;
        if (nsIdx < 0 || nsIdx >= _codeScanData.Types.Count) return;
        var ns = _addItemNamespaceOption.GetItemText(nsIdx);
        if (!_codeScanData.Types.ContainsKey(ns)) return;
        foreach (var type in _codeScanData.Types[ns].Keys)
            _addItemTypeParentOption.AddItem(type);
        if (_addItemTypeParentOption.ItemCount == 0)
            _addItemTypeParentOption.AddItem("<No types>");
    }

    private void OnAddItemConfirmPressed()
    {
        var selected = _addItemTypeOption.Selected;
        var name = _addItemNameEdit.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            _statusLabel.Text = "Name cannot be empty.";
            return;
        }
        if (selected == 0) // Namespace
        {
            if (!_codeScanData.Types.ContainsKey(name))
            {
                _codeScanData.Types[name] = new Dictionary<string, TypeData>();
                _statusLabel.Text = $"Added namespace '{name}'.";
            }
            else
            {
                _statusLabel.Text = $"Namespace '{name}' already exists.";
            }
        }
        else if (selected == 1) // Type
        {
            var nsIdx = _addItemNamespaceOption.Selected;
            if (nsIdx < 0) { _statusLabel.Text = "Select a namespace."; return; }
            var ns = _addItemNamespaceOption.GetItemText(nsIdx);
            if (!_codeScanData.Types.ContainsKey(ns)) { _statusLabel.Text = "Invalid namespace."; return; }
            if (!_codeScanData.Types[ns].ContainsKey(name))
            {
                var typeData = new TypeData();
                typeData.All = _addTypeAllCheckbox.ButtonPressed;
                _codeScanData.Types[ns][name] = typeData;
                _statusLabel.Text = $"Added type '{name}' to namespace '{ns}' (All: {typeData.All.ToString().ToLower()}).";
                if (!typeData.All)
                {
                    // Prompt to add methods/fields
                    _pendingAddNamespace = ns;
                    _pendingAddType = name;
                    _pendingAddTypePrompt = true;
                }
            }
            else
            {
                _statusLabel.Text = $"Type '{name}' already exists in '{ns}'.";
            }
        }
        else if (selected == 2 || selected == 3) // Method or Field
        {
            var nsIdx = _addItemNamespaceOption.Selected;
            var typeIdx = _addItemTypeParentOption.Selected;
            if (nsIdx < 0 || typeIdx < 0) { _statusLabel.Text = "Select a namespace and type."; return; }
            var ns = _addItemNamespaceOption.GetItemText(nsIdx);
            var type = _addItemTypeParentOption.GetItemText(typeIdx);
            if (!_codeScanData.Types.ContainsKey(ns) || !_codeScanData.Types[ns].ContainsKey(type))
            { _statusLabel.Text = "Invalid namespace or type."; return; }
            var typeData = _codeScanData.Types[ns][type];
            if (selected == 2) // Method
            {
                if (!typeData.Methods.Contains(name))
                {
                    typeData.Methods.Add(name);
                    _statusLabel.Text = $"Added method '{name}' to type '{type}' in '{ns}'.";
                }
                else
                {
                    _statusLabel.Text = $"Method '{name}' already exists in '{type}'.";
                }
            }
            else // Field
            {
                if (!typeData.Fields.Contains(name))
                {
                    typeData.Fields.Add(name);
                    _statusLabel.Text = $"Added field '{name}' to type '{type}' in '{ns}'.";
                }
                else
                {
                    _statusLabel.Text = $"Field '{name}' already exists in '{type}'.";
                }
            }
        }
        _addItemDialog.Hide();
        PopulateTree();
        UpdateStatistics();
        // If we need to prompt for methods/fields after adding a type
        if (_pendingAddTypePrompt)
        {
            _pendingAddTypePrompt = false;
            ShowAddMethodOrFieldForType(_pendingAddNamespace, _pendingAddType);
        }
    }

    private void ShowAddMethodOrFieldForType(string ns, string type)
    {
        // Reuse the add dialog, but lock namespace/type and only allow method/field
        _addItemTypeOption.Selected = 2; // Default to Method
        UpdateAddItemDialogUI();
        // Lock namespace/type selection
        _addItemNamespaceOption.Clear();
        _addItemNamespaceOption.AddItem(ns);
        _addItemNamespaceOption.Selected = 0;
        _addItemNamespaceOption.Disabled = true;
        _addItemTypeParentOption.Clear();
        _addItemTypeParentOption.AddItem(type);
        _addItemTypeParentOption.Selected = 0;
        _addItemTypeParentOption.Disabled = true;
        _addTypeAllCheckbox.Visible = false;
        _addItemDialog.Title = $"Add to {type} in {ns}";
        _addItemDialog.DialogText = "Add methods or fields to the new type. Close when done.";
        _addItemDialog.PopupCentered();
    }

    private void OnAddItemDialogHide()
    {
        _addItemNamespaceOption.Disabled = false;
        _addItemTypeParentOption.Disabled = false;
        _addItemDialog.Title = "Add New Item";
        _addItemDialog.DialogText = "Choose what you want to add and enter the name:";
    }

    // Update type options when namespace changes
    public override void _Process(double delta)
    {
        if (_addItemNamespaceOption.Visible && _addItemTypeParentOption.Visible)
        {
            _addItemNamespaceOption.ItemSelected -= OnNamespaceChangedForTypeParent;
            _addItemNamespaceOption.ItemSelected += OnNamespaceChangedForTypeParent;
        }
    }
    private void OnNamespaceChangedForTypeParent(long idx)
    {
        UpdateTypeParentOptions();
    }
}