using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;
using TMPro;

namespace US13.Core.Editor.Tools.TMPTools
{
    public class TMPEmojiFallbackGenerator : EditorWindow
    {
        private string _outputFolder = "Assets/Resources/Icons/Generated";
        private string _assetName = "GenEmoji";

        /// <summary>
        /// Maximum width/height of an individual source sprite.
        /// This also determines the size of each grid cell.
        /// </summary>
        private int _maxSpriteSize = 32;

        /// <summary>
        /// Transparent pixels between grid cells.
        /// </summary>
        private int _atlasPadding = 2;

        /// <summary>
        /// Maximum atlas dimension.
        /// </summary>
        private int _maxAtlasSize = 4096;

        private bool _includeSpritesOnly = true;

        /// <summary>
        /// If true, the generated asset becomes TMP's default sprite asset
        /// when no default sprite asset is currently configured.
        /// </summary>
        private bool _assignToTMPSettings = true;

        /// <summary>
        /// If true, add the generated sprite asset to the fallback list of
        /// TMP_Settings.defaultSpriteAsset.
        /// </summary>
        private bool _assignToDefaultSpriteAsset = true;

        private readonly List<Texture2D> _foundTextures = new List<Texture2D>();

        private List<RectInt> _generatedRects = new List<RectInt>();

        private Vector2 _scrollPos;

        private bool _hasScanned;

        private string _statusMessage = string.Empty;

        private MessageType _statusType = MessageType.Info;

        private enum ToolMode
        {
	        GenerateAtlas,
	        MassEditGlyphs
        }

        private ToolMode _toolMode = ToolMode.GenerateAtlas;

        private TMP_SpriteAsset _targetSpriteAsset;

        private float _glyphBearingX = 0;
        private float _glyphBearingY = 12;
        private float _glyphAdvance = 16;
        private float _glyphScale = 1.0f;


        [MenuItem("Tools/Assets/Emoji Fallback Generator")]
        public static void ShowWindow()
        {
            GetWindow<TMPEmojiFallbackGenerator>("TMP Emoji Tools");
        }

        private void OnGUI()
        {
	        EditorGUILayout.Space(10);
	        EditorGUILayout.LabelField("Tool Mode", EditorStyles.boldLabel);

	        _toolMode = (ToolMode)EditorGUILayout.EnumPopup(
		        "Mode",
		        _toolMode);

	        EditorGUILayout.Space(10);

	        if (_toolMode == ToolMode.MassEditGlyphs)
	        {
		        DrawMassEditGUI();
		        return;
	        }
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("TMP Emoji Fallback Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);

            if (GUILayout.Button("Select...", GUILayout.Width(70)))
            {
                string selected = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
                if (string.IsNullOrEmpty(selected) == false)
                {
                    if (selected.StartsWith(Application.dataPath, StringComparison.OrdinalIgnoreCase))
                    {
                        _outputFolder = "Assets" + selected.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        _statusMessage = "Please select a folder inside the Unity project.";
                        _statusType = MessageType.Warning;
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            _assetName = EditorGUILayout.TextField("Asset Name", _assetName);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Scan Settings", EditorStyles.boldLabel);
            _maxSpriteSize = Mathf.Max(1, EditorGUILayout.IntField("Max Sprite Size", _maxSpriteSize));
            _atlasPadding = Mathf.Max(0, EditorGUILayout.IntField("Grid Padding", _atlasPadding));
            _maxAtlasSize = Mathf.Max(_maxSpriteSize, EditorGUILayout.IntField("Max Atlas Size", _maxAtlasSize));

            _includeSpritesOnly = EditorGUILayout.Toggle("Sprite Assets Only", _includeSpritesOnly);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Fallback Assignment", EditorStyles.boldLabel);

            _assignToTMPSettings = EditorGUILayout.Toggle("Set TMP Default Sprite Asset", _assignToTMPSettings);
            _assignToDefaultSpriteAsset = EditorGUILayout.Toggle("Add To Default Sprite Fallbacks", _assignToDefaultSpriteAsset);

            EditorGUILayout.Space(15);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan Project", GUILayout.Height(30)))
            {
                ScanForSmallSprites();
            }
            GUI.enabled = _hasScanned && _foundTextures.Count > 0;
            if (GUILayout.Button("Generate Sprite Asset", GUILayout.Height(30)))
            {
                GenerateSpriteAsset();
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);

            if (string.IsNullOrEmpty(_statusMessage) == false)
            {
                EditorGUILayout.HelpBox(
                    _statusMessage,
                    _statusType);
            }

            if (_hasScanned)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField(
                    $"Found {_foundTextures.Count} sprite(s) <= " +
                    $"{_maxSpriteSize}x{_maxSpriteSize}",
                    EditorStyles.boldLabel);

                _scrollPos = EditorGUILayout.BeginScrollView(
                    _scrollPos,
                    GUILayout.MaxHeight(200));

                foreach (Texture2D texture in _foundTextures)
                {
                    if (texture != null)
                    {
                        EditorGUILayout.LabelField(
                            $"{texture.name} ({texture.width}x{texture.height})");
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawMassEditGUI()
        {
	        EditorGUILayout.LabelField("Mass Edit TMP Sprite Asset", EditorStyles.boldLabel);
	        EditorGUILayout.HelpBox("Select a TMP Sprite Asset. This will modify the metrics of every glyph in the asset without rebuilding the atlas.", MessageType.Info);

	        _targetSpriteAsset = (TMP_SpriteAsset)EditorGUILayout.ObjectField("Sprite Asset", _targetSpriteAsset, typeof(TMP_SpriteAsset), false);
	        EditorGUILayout.Space(10);
	        EditorGUILayout.LabelField("Glyph Metrics", EditorStyles.boldLabel);

	        _glyphBearingX = EditorGUILayout.FloatField("Bearing X", _glyphBearingX);

	        _glyphBearingY = EditorGUILayout.FloatField("Bearing Y", _glyphBearingY);

	        _glyphAdvance = EditorGUILayout.FloatField("Advance", _glyphAdvance);

	        _glyphScale = EditorGUILayout.FloatField("Scale", _glyphScale);

	        EditorGUILayout.Space(15);

	        GUI.enabled = _targetSpriteAsset != null;

	        if (GUILayout.Button("Mass Edit All Glyphs", GUILayout.Height(35)))
	        {
		        MassEditGlyphs();
	        }

	        GUI.enabled = true;

	        if (_targetSpriteAsset != null)
	        {
		        EditorGUILayout.Space(10);
		        EditorGUILayout.LabelField($"Glyphs: {_targetSpriteAsset.spriteGlyphTable.Count}", EditorStyles.boldLabel);
	        }
        }

        private void MassEditGlyphs()
        {
	        if (_targetSpriteAsset == null)
	        {
		        _statusMessage = "Please select a TMP Sprite Asset.";
		        _statusType = MessageType.Error;
		        return;
	        }

	        List<TMP_SpriteGlyph> glyphs = _targetSpriteAsset.spriteGlyphTable;
	        if (glyphs == null || glyphs.Count == 0)
	        {
		        _statusMessage = $"Sprite Asset '{_targetSpriteAsset.name}' contains no glyphs.";
		        _statusType = MessageType.Warning;
		        return;
	        }

	        Undo.RecordObject(_targetSpriteAsset, "Mass Edit TMP Sprite Glyphs");

	        for (int i = 0; i < glyphs.Count; i++)
	        {
		        TMP_SpriteGlyph oldGlyph = glyphs[i];
		        GlyphRect glyphRect = oldGlyph.glyphRect;
		        GlyphMetrics metrics =
			        new GlyphMetrics(
				        glyphRect.width,
				        glyphRect.height,
				        _glyphBearingX,
				        _glyphBearingY,
				        _glyphAdvance
			        );

		        TMP_SpriteGlyph newGlyph =
			        new TMP_SpriteGlyph(
				        oldGlyph.index,
				        metrics,
				        glyphRect,
				        _glyphScale,
				        oldGlyph.atlasIndex
			        );

		        glyphs[i] = newGlyph;
	        }

	        _targetSpriteAsset.UpdateLookupTables();

	        EditorUtility.SetDirty(_targetSpriteAsset);

	        AssetDatabase.SaveAssets();
	        AssetDatabase.Refresh();

	        _statusMessage = $"Successfully edited {glyphs.Count} glyph(s) " +
	                         $"in '{_targetSpriteAsset.name}'.\n" +
	                         $"Bearing: ({_glyphBearingX}, {_glyphBearingY})\n" +
	                         $"Advance: {_glyphAdvance}\n" +
	                         $"Scale: {_glyphScale}";

	        _statusType = MessageType.Info;

	        Selection.activeObject = _targetSpriteAsset;

	        EditorGUIUtility.PingObject(_targetSpriteAsset);
        }

        private void ScanForSmallSprites()
        {
            _foundTextures.Clear();
            _hasScanned = true;
            string[] guids;
            guids = AssetDatabase.FindAssets(_includeSpritesOnly ? "t:Sprite" : "t:Texture2D", new[] { "Assets" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                if (texture == null) continue;

                if (texture.width > _maxSpriteSize || texture.height > _maxSpriteSize)
                {
                    continue;
                }

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer != null && importer.isReadable == false)
                {
                    continue;
                }

                _foundTextures.Add(texture);
            }

            // Keep generation deterministic.
            _foundTextures.Sort(
	            (a, b) => string.Compare(
                        AssetDatabase.GetAssetPath(a),
                        AssetDatabase.GetAssetPath(b),
                        StringComparison.OrdinalIgnoreCase
                        )
	            );

            if (_foundTextures.Count > 0)
            {
                _statusMessage = $"Scan complete. Found {_foundTextures.Count} " +
                                 $"qualifying sprite(s).";
                _statusType = MessageType.Info;
            }
            else
            {
                _statusMessage = "No qualifying sprites found. " +
                                 "Make sure the textures are <= Max Sprite Size " +
                                 "and have Read/Write enabled.";
                _statusType = MessageType.Warning;
            }
        }

        private void GenerateSpriteAsset()
        {
            if (_foundTextures.Count == 0)
            {
                _statusMessage = "No sprites to process. Run Scan first.";
                _statusType = MessageType.Error;
                return;
            }

            if (ValidateSettings() == false) return;
            EnsureOutputFolder();

            Texture2D atlas = BuildGridAtlas(_foundTextures, out _generatedRects);

            if (atlas == null)
            {
                _statusMessage = "Failed to generate the atlas.";
                _statusType = MessageType.Error;
                return;
            }

            string atlasPath = NormalizePath(Path.Combine(_outputFolder, _assetName + "_Atlas.png"));
            byte[] pngData = atlas.EncodeToPNG();
            if (pngData == null || pngData.Length == 0)
            {
                DestroyImmediate(atlas);
                _statusMessage = "Failed to encode atlas PNG.";
                _statusType = MessageType.Error;
                return;
            }

            File.WriteAllBytes(atlasPath, pngData);
            DestroyImmediate(atlas);
            AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceUpdate);
            ConfigureAtlasImporter(atlasPath, _foundTextures, _generatedRects);
            Texture2D finalAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);

            if (finalAtlas == null)
            {
                _statusMessage = $"Could not load generated atlas: {atlasPath}";
                _statusType = MessageType.Error;
                return;
            }

            TMP_SpriteAsset spriteAsset = CreateTMP_SpriteAsset(finalAtlas, _foundTextures, _generatedRects);

            if (spriteAsset == null)
            {
                _statusMessage = "Failed to create TMP_SpriteAsset.";
                _statusType = MessageType.Error;
                return;
            }

            string assetPath = NormalizePath(Path.Combine(_outputFolder, _assetName + ".asset"));

            // Remove an existing asset if one exists.
            TMP_SpriteAsset existing = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(assetPath);

            if (existing != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            AssetDatabase.CreateAsset(spriteAsset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int fallbackCount = ConfigureTMPFallbacks(spriteAsset);

            _statusMessage = $"Successfully generated TMP Sprite Asset.\n" +
                             $"Asset: {assetPath}\n" +
                             $"Atlas: {atlasPath}\n" +
                             $"Sprites: {_foundTextures.Count}\n" +
                             $"Grid: {GetGridDescription(_foundTextures.Count)}\n" +
                             $"Atlas Size: {finalAtlas.width}x{finalAtlas.height}\n" +
                             $"Fallback Assignments: {fallbackCount}";

            _statusType = MessageType.Info;

            Selection.activeObject = spriteAsset;

            EditorGUIUtility.PingObject(spriteAsset);
        }

        private bool ValidateSettings()
        {
            if (_maxSpriteSize <= 0)
            {
                _statusMessage = "Max Sprite Size must be greater than zero.";
                _statusType = MessageType.Error;
                return false;
            }

            if (_maxAtlasSize <= 0)
            {
                _statusMessage = "Max Atlas Size must be greater than zero.";
                _statusType = MessageType.Error;
                return false;
            }

            if (_maxSpriteSize > _maxAtlasSize)
            {
                _statusMessage = "Max Sprite Size cannot be larger than Max Atlas Size.";
                _statusType = MessageType.Error;
                return false;
            }

            if (string.IsNullOrWhiteSpace(_assetName))
            {
                _statusMessage = "Asset Name cannot be empty.";
                _statusType = MessageType.Error;
                return false;
            }

            return true;
        }

        private void EnsureOutputFolder()
        {
            if (AssetDatabase.IsValidFolder(_outputFolder)) return;
            string[] folders = _outputFolder.Split('/');
            string current = folders[0];
            for (int i = 1; i < folders.Length; i++)
            {
                string next = current + "/" + folders[i];

                if (AssetDatabase.IsValidFolder(next) == false)
                {
                    AssetDatabase.CreateFolder(current, folders[i]);
                }

                current = next;
            }

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Creates a deterministic grid atlas.
        /// The generated grid will be approximately:
        /// 4 columns x 3 rows
        /// Every cell is large enough to contain a 32x32 sprite.
        /// No source texture is scaled.
        /// </summary>
        private Texture2D BuildGridAtlas(List<Texture2D> textures, out List<RectInt> spriteRects)
        {
            spriteRects = new List<RectInt>();
            if (textures == null || textures.Count == 0) return null;
            int count = textures.Count;
            // Make the grid as square as possible.
            int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
            columns = Mathf.Max(1, columns);
            int rows = Mathf.CeilToInt((float)count / columns);
            int cellSize = _maxSpriteSize;
            int cellWithPadding = cellSize + _atlasPadding;
            int atlasWidth = _atlasPadding + columns * cellWithPadding;
            int atlasHeight = _atlasPadding + rows * cellWithPadding;

            // Keep the dimensions power-of-two where possible for determinism grid.
            atlasWidth = Mathf.NextPowerOfTwo(atlasWidth);
            atlasHeight = Mathf.NextPowerOfTwo(atlasHeight);

            if (atlasWidth > _maxAtlasSize || atlasHeight > _maxAtlasSize)
            {
                _statusMessage = $"The sprites require a {atlasWidth}x{atlasHeight} " +
                                 $"atlas, which exceeds the Max Atlas Size of " +
                                 $"{_maxAtlasSize}.\n\n" +
                                 $"Reduce the number of sprites, increase Max Atlas Size, " +
                                 $"or reduce Max Sprite Size.";

                _statusType = MessageType.Error;
                return null;
            }

            Texture2D atlas = new Texture2D(atlasWidth, atlasHeight, TextureFormat.RGBA32, false);

            atlas.name = _assetName + "_Atlas";
            atlas.filterMode = FilterMode.Point;
            atlas.wrapMode = TextureWrapMode.Clamp;

            Color32[] clearPixels = new Color32[atlasWidth * atlasHeight];

            for (int i = 0; i < clearPixels.Length; i++)
            {
                clearPixels[i] = new Color32(0, 0, 0, 0);
            }

            atlas.SetPixels32(clearPixels);

            for (int i = 0; i < textures.Count; i++)
            {
                Texture2D source = textures[i];
                if (source == null) continue;

                int column = i % columns;
                int row = i / columns;
                int cellX = _atlasPadding + column * cellWithPadding;
                int cellY = atlasHeight - _atlasPadding - ((row + 1) * cellWithPadding);

                // Center the actual sprite inside the fixed-size cell.
                int x = cellX + Mathf.FloorToInt((cellSize - source.width) * 0.5f);
                int y = cellY + Mathf.FloorToInt((cellSize - source.height) * 0.5f);

                RectInt rect = new RectInt(
                    x,
                    y,
                    source.width,
                    source.height
                    );

                spriteRects.Add(rect);

                Color32[] sourcePixels =
                    source.GetPixels32();

                atlas.SetPixels32(
                    rect.x,
                    rect.y,
                    rect.width,
                    rect.height,
                    sourcePixels
                    );
            }

            atlas.Apply(
                updateMipmaps: false,
                makeNoLongerReadable: false);

            return atlas;
        }


        private void ConfigureAtlasImporter(string atlasPath, List<Texture2D> sourceTextures, List<RectInt> spriteRects)
        {
	        var importer = AssetImporter.GetAtPath(atlasPath) as TextureImporter;

	        if (importer == null)
	        {
		        Debug.LogError($"Could not find TextureImporter for {atlasPath}");
		        return;
	        }

	        if (sourceTextures == null ||
	            spriteRects == null ||
	            sourceTextures.Count != spriteRects.Count)
	        {
		        Debug.LogError("Source texture count does not match sprite rectangle count.");
		        return;
	        }

	        importer.textureType = TextureImporterType.Sprite;
	        importer.spriteImportMode = SpriteImportMode.Multiple;
	        importer.spritePixelsPerUnit = _maxSpriteSize;
	        importer.isReadable = true;
	        importer.textureCompression = TextureImporterCompression.Uncompressed;
	        importer.filterMode = FilterMode.Point;
	        importer.wrapMode = TextureWrapMode.Clamp;
	        importer.mipmapEnabled = false;

	        var spriteMetadata = new SpriteMetaData[spriteRects.Count];

	        for (var i = 0; i < spriteRects.Count; i++)
	        {
		        Texture2D source = sourceTextures[i];
		        RectInt rect = spriteRects[i];
		        var metadata = new SpriteMetaData();

		        metadata.name = source.name;

		        metadata.rect = new Rect(
				        rect.x,
				        rect.y,
				        rect.width,
				        rect.height
			        );

		        metadata.pivot = new Vector2(0.5f, 0.5f);
		        metadata.alignment = (int)SpriteAlignment.Center;
		        metadata.border = Vector4.zero;
		        spriteMetadata[i] = metadata;
	        }

	        // Tell Unity exactly where every Sprite is located
	        // inside the generated atlas.
	        importer.spritesheet = spriteMetadata;

	        // Save and reimport so Unity creates the Sprite sub-assets.
	        importer.SaveAndReimport();
        }


        private TMP_SpriteAsset CreateTMP_SpriteAsset(Texture2D atlas, List<Texture2D> sourceTextures, List<RectInt> spriteRects)
        {
            if (atlas == null) return null;
            if (sourceTextures == null || spriteRects == null || sourceTextures.Count != spriteRects.Count)
            {
                Debug.LogError("Source texture count does not match atlas rect count.");
                return null;
            }

            TMP_SpriteAsset spriteAsset = CreateInstance<TMP_SpriteAsset>();

            spriteAsset.name = _assetName;
            spriteAsset.spriteSheet = atlas;

            // Current TMP versions expose these lists directly.
            // Do NOT use SerializedProperty for these tables.
            spriteAsset.spriteCharacterTable.Clear();
            spriteAsset.spriteGlyphTable.Clear();

            var tmpMat = new Material(Shader.Find("TextMeshPro/Sprite"));
            if (tmpMat != null)
            {
	            Material material = tmpMat;
	            material.name = _assetName + "_Material";
	            material.SetTexture(ShaderUtilities.ID_MainTex, atlas);
	            spriteAsset.material = material;
            }

            for (int i = 0; i < sourceTextures.Count; i++)
            {
                Texture2D source = sourceTextures[i];
                RectInt rect = spriteRects[i];
                if (source == null) continue;
                uint glyphIndex = (uint)i;

                //DON'T MESS WITH THESE HARDCODED NUMBERS AAAAAAAAAAAAAAAA
                GlyphMetrics metrics =
                    new GlyphMetrics(
                        source.width,
                        source.height,
                        0,
                        12,
                        16
                        );

                GlyphRect glyphRect =
                    new GlyphRect(
                        rect.x,
                        rect.y,
                        rect.width,
                        rect.height
                        );

                TMP_SpriteGlyph glyph =
                    new TMP_SpriteGlyph
                    (
                        glyphIndex,
                        metrics,
                        glyphRect,
                        1.0f,
                        0
                        );

                TMP_SpriteCharacter character = new TMP_SpriteCharacter(0, glyph);

                character.name = source.name;
                character.scale = 1.0f;
                spriteAsset.spriteGlyphTable.Add(glyph);
                spriteAsset.spriteCharacterTable.Add(character);
            }

            // Rebuild TMP's name/unicode lookup dictionaries.
            try
            {
	            spriteAsset.UpdateLookupTables();
            }
            catch (Exception e)
            {
	            Debug.LogError($"Failed to update TMP lookup tables: {e.Message}");
            }

            return spriteAsset;
        }

        private int ConfigureTMPFallbacks(
            TMP_SpriteAsset generatedSpriteAsset)
        {
            if (generatedSpriteAsset == null)
                return 0;

            int count = 0;

            TMP_Settings settings = TMP_Settings.GetSettings();

            if (settings == null)
            {
                Debug.LogWarning("TMP_Settings could not be found.");
                return count;
            }

            if (_assignToTMPSettings)
            {
                SerializedObject settingsObject = new SerializedObject(settings);

                SerializedProperty defaultSpriteAssetProperty = settingsObject.FindProperty("m_defaultSpriteAsset");

                if (defaultSpriteAssetProperty != null)
                {
                    if (defaultSpriteAssetProperty.objectReferenceValue == null)
                    {
                        defaultSpriteAssetProperty.objectReferenceValue = generatedSpriteAsset;
                        settingsObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(settings);
                        count++;
                    }
                }
                else
                {
                    Debug.LogWarning("TMP_Settings does not contain the expected " +
                                     "serialized property 'm_defaultSpriteAsset'.");
                }
                settingsObject.Dispose();
            }

            if (_assignToDefaultSpriteAsset)
            {
                TMP_SpriteAsset defaultSpriteAsset = TMP_Settings.defaultSpriteAsset;
                if (defaultSpriteAsset != null)
                {
	                defaultSpriteAsset.fallbackSpriteAssets ??= new List<TMP_SpriteAsset>();
	                if (defaultSpriteAsset.fallbackSpriteAssets.Contains(generatedSpriteAsset) == false)
                    {
                        defaultSpriteAsset.fallbackSpriteAssets.Add(generatedSpriteAsset);
                        EditorUtility.SetDirty(defaultSpriteAsset);
                        count++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            return count;
        }

        private string GetGridDescription(int spriteCount)
        {
            if (spriteCount <= 0)
                return "0x0";

            int columns =
                Mathf.CeilToInt(
                    Mathf.Sqrt(spriteCount));

            int rows =
                Mathf.CeilToInt(
                    (float)spriteCount / columns);

            return $"{columns}x{rows}";
        }

        private static string NormalizePath(string path)
        {
            return path.Replace("\\", "/");
        }

        private Sprite[] GetGeneratedSprites(string atlasPath)
        {
	        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(atlasPath);

	        List<Sprite> sprites = new List<Sprite>();

	        foreach (UnityEngine.Object asset in assets)
	        {
		        if (asset is Sprite sprite)
		        {
			        sprites.Add(sprite);
		        }
	        }

	        sprites.Sort(
		        (a, b) =>
			        string.Compare(
				        a.name,
				        b.name,
				        StringComparison.OrdinalIgnoreCase)
			        );

	        return sprites.ToArray();
        }
    }
}
