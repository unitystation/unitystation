using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
#if UNITY_2017_3_OR_NEWER
using UnityEditor.Compilation;
#endif
using UnityEngine;
using Object = UnityEngine.Object;

namespace TextToTMPNamespace
{
	public partial class TextToTMPWindow
	{
		#region Helper Classes
#pragma warning disable 0649
#if UNITY_2017_3_OR_NEWER
		// Reference: https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html
		[Serializable]
		private class AssemblyDefinitionFileContents
		{
			public string name;
			public string[] references;
			public string[] includePlatforms;
			public string[] excludePlatforms;
			public bool allowUnsafeCode;
			public bool autoReferenced = true;
			public bool overrideReferences;
			public string[] precompiledReferences;
			public string[] defineConstraints;
			public string[] optionalUnityReferences;
		}
#endif
#pragma warning restore 0649
		#endregion

		#region Constants
		private const string EXTENSION_FUNCTIONS_NAMESPACE = "namespace TextToTMPNamespace.Instance";
		private const string EXTENSION_FUNCTIONS_NAMESPACE_DECLARATION = @"
using TextToTMPNamespace.Instance{0};
namespace TextToTMPNamespace.Instance{0}";

		private const string EXTENSION_FUNCTIONS_BODY = @"
{
	using UnityEngine;
	using TMPro;

	internal static class TextToTMPExtensions
	{
		public static void SetTMPAlignment( this TMP_Text tmp, TextAlignmentOptions alignment )
		{
			tmp.alignment = alignment;
		}

		public static void SetTMPAlignment( this TMP_Text tmp, TextAnchor alignment )
		{
			switch( alignment )
			{
				case TextAnchor.LowerLeft: tmp.alignment = TextAlignmentOptions.BottomLeft; break;
				case TextAnchor.LowerCenter: tmp.alignment = TextAlignmentOptions.Bottom; break;
				case TextAnchor.LowerRight: tmp.alignment = TextAlignmentOptions.BottomRight; break;
				case TextAnchor.MiddleLeft: tmp.alignment = TextAlignmentOptions.Left; break;
				case TextAnchor.MiddleCenter: tmp.alignment = TextAlignmentOptions.Center; break;
				case TextAnchor.MiddleRight: tmp.alignment = TextAlignmentOptions.Right; break;
				case TextAnchor.UpperLeft: tmp.alignment = TextAlignmentOptions.TopLeft; break;
				case TextAnchor.UpperCenter: tmp.alignment = TextAlignmentOptions.Top; break;
				case TextAnchor.UpperRight: tmp.alignment = TextAlignmentOptions.TopRight; break;
				default: tmp.alignment = TextAlignmentOptions.Center; break;
			}
		}

		public static void SetTMPFontStyle( this TMP_Text tmp, FontStyles fontStyle )
		{
			tmp.fontStyle = fontStyle;
		}

		public static void SetTMPFontStyle( this TMP_Text tmp, FontStyle fontStyle )
		{
			FontStyles fontStyles;
			switch( fontStyle )
			{
				case FontStyle.Bold: fontStyles = FontStyles.Bold; break;
				case FontStyle.Italic: fontStyles = FontStyles.Italic; break;
				case FontStyle.BoldAndItalic: fontStyles = FontStyles.Bold | FontStyles.Italic; break;
				default: fontStyles = FontStyles.Normal; break;
			}

			tmp.fontStyle = fontStyles;
		}

		public static void SetTMPHorizontalOverflow( this TMP_Text tmp, HorizontalWrapMode overflow )
		{
			tmp.enableWordWrapping = ( overflow == HorizontalWrapMode.Wrap );
		}

		public static HorizontalWrapMode GetTMPHorizontalOverflow( this TMP_Text tmp )
		{
			return tmp.enableWordWrapping ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
		}

		public static void SetTMPVerticalOverflow( this TMP_Text tmp, TextOverflowModes overflow )
		{
			tmp.overflowMode = overflow;
		}

		public static void SetTMPVerticalOverflow( this TMP_Text tmp, VerticalWrapMode overflow )
		{
			tmp.overflowMode = ( overflow == VerticalWrapMode.Overflow ) ? TextOverflowModes.Overflow : TextOverflowModes.Truncate;
		}

		public static void SetTMPLineSpacing( this TMP_Text tmp, float lineSpacing )
		{
			tmp.lineSpacing = ( lineSpacing - 1 ) * 100f;
		}

		public static void SetTMPCaretWidth( this TMP_InputField tmp, int caretWidth )
		{
			tmp.caretWidth = caretWidth;
		}

		public static void SetTMPCaretWidth( this TMP_InputField tmp, float caretWidth )
		{
			tmp.caretWidth = (int) caretWidth;
		}
	}
}

";

		private readonly string[] tokenReplacementsVariableDeclarations = new string[]
		{
			"UnityEngine.UI.Text", "TMPro.TMP_Text",
			"Text", "TMPro.TMP_Text",
			"UnityEngine.UI.InputField", "TMPro.TMP_InputField",
			"InputField", "TMPro.TMP_InputField",
			"UnityEngine.UI.Dropdown", "TMPro.TMP_Dropdown",
			"Dropdown", "TMPro.TMP_Dropdown",
			"UnityEngine.TextMesh", "TMPro.TMP_Text",
			"TextMesh", "TMPro.TMP_Text",
			"UnityEngine.Font", "TMPro.TMP_FontAsset",
			"Font", "TMPro.TMP_FontAsset",
			"UnityEngine.FontStyle", "TMPro.FontStyles",
			"FontStyle", "TMPro.FontStyles",
			"UnityEngine.TextAnchor", "TMPro.TextAlignmentOptions",
			"TextAnchor", "TMPro.TextAlignmentOptions",
		};

		private readonly string[] tokenReplacementsWithTMPChecks = new string[]
		{
			"UnityEngine.UI.Dropdown.OptionData", "TMPro.TMP_Dropdown.OptionData",
			"Dropdown.OptionData", "TMPro.TMP_Dropdown.OptionData",
			"UnityEngine.UI.InputField.CharacterValidation", "TMPro.TMP_InputField.CharacterValidation",
			"InputField.CharacterValidation", "TMPro.TMP_InputField.CharacterValidation",
			"UnityEngine.UI.InputField.ContentType", "TMPro.TMP_InputField.ContentType",
			"InputField.ContentType", "TMPro.TMP_InputField.ContentType",
			"UnityEngine.UI.InputField.InputType", "TMPro.TMP_InputField.InputType",
			"InputField.InputType", "TMPro.TMP_InputField.InputType",
			"UnityEngine.UI.InputField.LineType", "TMPro.TMP_InputField.LineType",
			"InputField.LineType", "TMPro.TMP_InputField.LineType",
			"UnityEngine.UI.InputField.SubmitEvent", "TMPro.TMP_InputField.SubmitEvent",
			"InputField.SubmitEvent", "TMPro.TMP_InputField.SubmitEvent",
			"UnityEngine.UI.InputField.OnValidateInput", "TMPro.TMP_InputField.OnValidateInput",
			"InputField.OnValidateInput", "TMPro.TMP_InputField.OnValidateInput",
			"UnityEngine.UI.InputField.OnChangeEvent", "TMPro.TMP_InputField.OnChangeEvent",
			"InputField.OnChangeEvent", "TMPro.TMP_InputField.OnChangeEvent",
		};

		private readonly string[] tokenReplacementsAsIs = new string[]
		{
			"UnityEngine.FontStyle.BoldAndItalic", "( TMPro.FontStyles.Bold | TMPro.FontStyles.Italic )",
			"FontStyle.BoldAndItalic", "( TMPro.FontStyles.Bold | TMPro.FontStyles.Italic )",
			"UnityEngine.FontStyle.", "TMPro.FontStyles.",
			"FontStyle.", "TMPro.FontStyles.",
			"UnityEngine.TextAnchor.LowerLeft", "TMPro.TextAlignmentOptions.BottomLeft",
			"TextAnchor.LowerLeft", "TMPro.TextAlignmentOptions.BottomLeft",
			"UnityEngine.TextAnchor.LowerCenter", "TMPro.TextAlignmentOptions.Bottom",
			"TextAnchor.LowerCenter", "TMPro.TextAlignmentOptions.Bottom",
			"UnityEngine.TextAnchor.LowerRight", "TMPro.TextAlignmentOptions.BottomRight",
			"TextAnchor.LowerRight", "TMPro.TextAlignmentOptions.BottomRight",
			"UnityEngine.TextAnchor.MiddleLeft", "TMPro.TextAlignmentOptions.Left",
			"TextAnchor.MiddleLeft", "TMPro.TextAlignmentOptions.Left",
			"UnityEngine.TextAnchor.MiddleCenter", "TMPro.TextAlignmentOptions.Center",
			"TextAnchor.MiddleCenter", "TMPro.TextAlignmentOptions.Center",
			"UnityEngine.TextAnchor.MiddleRight", "TMPro.TextAlignmentOptions.Right",
			"TextAnchor.MiddleRight", "TMPro.TextAlignmentOptions.Right",
			"UnityEngine.TextAnchor.UpperLeft", "TMPro.TextAlignmentOptions.TopLeft",
			"TextAnchor.UpperLeft", "TMPro.TextAlignmentOptions.TopLeft",
			"UnityEngine.TextAnchor.UpperCenter", "TMPro.TextAlignmentOptions.Top",
			"TextAnchor.UpperCenter", "TMPro.TextAlignmentOptions.Top",
			"UnityEngine.TextAnchor.UpperRight", "TMPro.TextAlignmentOptions.TopRight",
			"TextAnchor.UpperRight", "TMPro.TextAlignmentOptions.TopRight",
			"UnityEngine.VerticalWrapMode.Truncate", "TMPro.TextOverflowModes.Truncate",
			"VerticalWrapMode.Truncate", "TMPro.TextOverflowModes.Truncate",
			"UnityEngine.VerticalWrapMode.Overflow", "TMPro.TextOverflowModes.Overflow",
			"VerticalWrapMode.Overflow", "TMPro.TextOverflowModes.Overflow",
			".resizeTextForBestFit", ".enableAutoSizing",
			".resizeTextMinSize", ".fontSizeMin",
			".resizeTextMaxSize", ".fontSizeMax",
		};

		private readonly string[] tokenReplacementsExtensionFunctions = new string[]
		{
			".alignment =", ".SetTMPAlignment(", " )",
			".alignment=", ".SetTMPAlignment(", ")",
			".fontStyle =", ".SetTMPFontStyle(", " )",
			".fontStyle=", ".SetTMPFontStyle(", ")",
			".horizontalOverflow =", ".SetTMPHorizontalOverflow(", " )",
			".horizontalOverflow=", ".SetTMPHorizontalOverflow(", ")",
			".horizontalOverflow", ".GetTMPHorizontalOverflow(", ")",
			".verticalOverflow =", ".SetTMPVerticalOverflow(", " )",
			".verticalOverflow=", ".SetTMPVerticalOverflow(", ")",
			".lineSpacing =", ".SetTMPLineSpacing(", " )",
			".lineSpacing=", ".SetTMPLineSpacing(", ")",
			".caretWidth =", ".SetTMPCaretWidth(", " )",
			".caretWidth=", ".SetTMPCaretWidth(", ")",
		};
		#endregion

#if UNITY_2017_3_OR_NEWER
		private string textMeshProAssemblyDefinitionName;
		private string textMeshProAssemblyDefinitionFilePath;
#endif

		private ObjectsToUpgradeList scriptsToUpgrade = new ObjectsToUpgradeList();
#if UNITY_2017_3_OR_NEWER
		private ObjectsToUpgradeList assemblyDefinitionFilesToUpgrade = new ObjectsToUpgradeList();
#endif

		private void UpgradeScripts()
		{
#if UNITY_2017_3_OR_NEWER
			textMeshProAssemblyDefinitionFilePath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName( "Unity.TextMeshPro" );
			if( !string.IsNullOrEmpty( textMeshProAssemblyDefinitionFilePath ) )
				textMeshProAssemblyDefinitionName = "Unity.TextMeshPro";
			else
			{
				string[] tmpScriptGuid = AssetDatabase.FindAssets( "TextMeshProUGUI t:MonoScript" );
				if( tmpScriptGuid != null && tmpScriptGuid.Length > 0 )
				{
					textMeshProAssemblyDefinitionName = CompilationPipeline.GetAssemblyNameFromScriptPath( AssetDatabase.GUIDToAssetPath( tmpScriptGuid[0] ) );

					if( !string.IsNullOrEmpty( textMeshProAssemblyDefinitionName ) )
						textMeshProAssemblyDefinitionFilePath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName( textMeshProAssemblyDefinitionName );
				}
			}
#endif

			stringBuilder.Length = 0;

			int progressCurrent = 0;
#if UNITY_2017_3_OR_NEWER
			int progressTotal = scriptsToUpgrade.EnabledCount + assemblyDefinitionFilesToUpgrade.EnabledCount;
#else
			int progressTotal = scriptsToUpgrade.EnabledCount;
#endif

			try
			{
				foreach( string script in scriptsToUpgrade )
				{
					EditorUtility.DisplayProgressBar( "Upgrading scripts...", script, (float) progressCurrent / progressTotal );
					progressCurrent++;

					UpgradeScript( script );
				}

#if UNITY_2017_3_OR_NEWER
				foreach( string assemblyDefinitionFile in assemblyDefinitionFilesToUpgrade )
				{
					EditorUtility.DisplayProgressBar( "Upgrading Assembly Definition Files...", assemblyDefinitionFile, (float) progressCurrent / progressTotal );
					progressCurrent++;

					UpgradeAssemblyDefinitionFile( assemblyDefinitionFile );
				}
#endif
			}
			finally
			{
				if( stringBuilder.Length > 0 )
				{
					stringBuilder.Insert( 0, "<b>Upgrade Scripts Logs:</b>\n" );
					Debug.Log( stringBuilder.ToString() );
				}

				EditorUtility.ClearProgressBar();
				AssetDatabase.Refresh();
			}
		}

		private void UpgradeScript( string scriptPath )
		{
			string script = File.ReadAllText( scriptPath );
			string scriptModified = script;

			// This script has custom extension functions added by this editor script, so it has already been upgraded before
			if( scriptModified.Contains( EXTENSION_FUNCTIONS_NAMESPACE ) )
				return;

			for( int i = 0; i < tokenReplacementsVariableDeclarations.Length; i += 2 )
			{
				// Variable declarations
				scriptModified = scriptModified.Replace( "public " + tokenReplacementsVariableDeclarations[i] + " ", "public " + tokenReplacementsVariableDeclarations[i + 1] + " " );
				scriptModified = scriptModified.Replace( "private " + tokenReplacementsVariableDeclarations[i] + " ", "private " + tokenReplacementsVariableDeclarations[i + 1] + " " );
				scriptModified = scriptModified.Replace( "protected " + tokenReplacementsVariableDeclarations[i] + " ", "protected " + tokenReplacementsVariableDeclarations[i + 1] + " " );
				scriptModified = scriptModified.Replace( "internal " + tokenReplacementsVariableDeclarations[i] + " ", "internal " + tokenReplacementsVariableDeclarations[i + 1] + " " );
				scriptModified = scriptModified.Replace( "static " + tokenReplacementsVariableDeclarations[i] + " ", "static " + tokenReplacementsVariableDeclarations[i + 1] + " " );
				scriptModified = scriptModified.Replace( "readonly " + tokenReplacementsVariableDeclarations[i] + " ", "readonly " + tokenReplacementsVariableDeclarations[i + 1] + " " );
				scriptModified = scriptModified.Replace( "virtual " + tokenReplacementsVariableDeclarations[i] + " ", "virtual " + tokenReplacementsVariableDeclarations[i + 1] + " " );
				scriptModified = scriptModified.Replace( "volatile " + tokenReplacementsVariableDeclarations[i] + " ", "volatile " + tokenReplacementsVariableDeclarations[i + 1] + " " );
				scriptModified = scriptModified.Replace( "sealed " + tokenReplacementsVariableDeclarations[i] + " ", "sealed " + tokenReplacementsVariableDeclarations[i + 1] + " " );
				scriptModified = scriptModified.Replace( "partial " + tokenReplacementsVariableDeclarations[i] + " ", "partial " + tokenReplacementsVariableDeclarations[i + 1] + " " );
				scriptModified = scriptModified.Replace( "extern " + tokenReplacementsVariableDeclarations[i] + " ", "extern " + tokenReplacementsVariableDeclarations[i + 1] + " " );
				// Variable declarations without modifier (1st: indentation with tabs, 2nd: indentation with spaces)
				scriptModified = scriptModified.Replace( "\t" + tokenReplacementsVariableDeclarations[i] + " ", "\t" + tokenReplacementsVariableDeclarations[i + 1] + " " );
				scriptModified = scriptModified.Replace( "  " + tokenReplacementsVariableDeclarations[i] + " ", "  " + tokenReplacementsVariableDeclarations[i + 1] + " " );
				// Arrays
				scriptModified = scriptModified.Replace( "\t" + tokenReplacementsVariableDeclarations[i] + "[]", "\t" + tokenReplacementsVariableDeclarations[i + 1] + "[]" );
				scriptModified = scriptModified.Replace( " " + tokenReplacementsVariableDeclarations[i] + "[]", " " + tokenReplacementsVariableDeclarations[i + 1] + "[]" );
				scriptModified = scriptModified.Replace( " new " + tokenReplacementsVariableDeclarations[i] + "[", " new " + tokenReplacementsVariableDeclarations[i + 1] + "[" );
				// Generics
				scriptModified = scriptModified.Replace( "<" + tokenReplacementsVariableDeclarations[i] + ">", "<" + tokenReplacementsVariableDeclarations[i + 1] + ">" );
				scriptModified = scriptModified.Replace( "< " + tokenReplacementsVariableDeclarations[i] + " >", "< " + tokenReplacementsVariableDeclarations[i + 1] + " >" );
				// Function declarations
				scriptModified = scriptModified.Replace( "(" + tokenReplacementsVariableDeclarations[i] + " ", "(" + tokenReplacementsVariableDeclarations[i + 1] + " " );
				scriptModified = scriptModified.Replace( "( " + tokenReplacementsVariableDeclarations[i] + " ", "( " + tokenReplacementsVariableDeclarations[i + 1] + " " );
				scriptModified = scriptModified.Replace( "," + tokenReplacementsVariableDeclarations[i] + " ", "," + tokenReplacementsVariableDeclarations[i + 1] + " " );
				scriptModified = scriptModified.Replace( ", " + tokenReplacementsVariableDeclarations[i] + " ", ", " + tokenReplacementsVariableDeclarations[i + 1] + " " );
				// Typecasts
				scriptModified = scriptModified.Replace( "(" + tokenReplacementsVariableDeclarations[i] + ")", "(" + tokenReplacementsVariableDeclarations[i + 1] + ")" );
				scriptModified = scriptModified.Replace( "( " + tokenReplacementsVariableDeclarations[i] + " )", "( " + tokenReplacementsVariableDeclarations[i + 1] + " )" );
				scriptModified = scriptModified.Replace( " is " + tokenReplacementsVariableDeclarations[i] + " ", " is " + tokenReplacementsVariableDeclarations[i + 1] + " " );
				scriptModified = scriptModified.Replace( " as " + tokenReplacementsVariableDeclarations[i] + " ", " as " + tokenReplacementsVariableDeclarations[i + 1] + " " );
				scriptModified = scriptModified.Replace( " is " + tokenReplacementsVariableDeclarations[i] + ")", " is " + tokenReplacementsVariableDeclarations[i + 1] + ")" );
				scriptModified = scriptModified.Replace( " as " + tokenReplacementsVariableDeclarations[i] + ")", " as " + tokenReplacementsVariableDeclarations[i + 1] + ")" );
				scriptModified = scriptModified.Replace( " is " + tokenReplacementsVariableDeclarations[i] + ";", " is " + tokenReplacementsVariableDeclarations[i + 1] + ";" );
				scriptModified = scriptModified.Replace( " as " + tokenReplacementsVariableDeclarations[i] + ";", " as " + tokenReplacementsVariableDeclarations[i + 1] + ";" );
				// Extensions functions
				scriptModified = scriptModified.Replace( "this " + tokenReplacementsVariableDeclarations[i] + " ", "this " + tokenReplacementsVariableDeclarations[i + 1] + " " );
				// Instantiation
				scriptModified = scriptModified.Replace( "new " + tokenReplacementsVariableDeclarations[i] + "(", "new " + tokenReplacementsVariableDeclarations[i + 1] + "(" );
				scriptModified = scriptModified.Replace( "new " + tokenReplacementsVariableDeclarations[i] + " (", "new " + tokenReplacementsVariableDeclarations[i + 1] + " (" );
			}

			// This is an edge case that occurs with the previous loop when a string variable called Text is defined in the script
			scriptModified = scriptModified.Replace( "TMPro.TMP_Text =", "Text =" );

			for( int i = 0; i < tokenReplacementsWithTMPChecks.Length; i += 2 )
			{
				// We want to convert "Dropdown.OptionData" to "TMPro.TMP_Dropdown.OptionData" but we don't want to convert "TMPro.TMP_Dropdown.OptionData" to "TMPro.TMP_TMPro.TMP_Dropdown.OptionData"
				int index = -1;
				while( ( index = scriptModified.IndexOf( tokenReplacementsWithTMPChecks[i], index + 1 ) ) >= 0 )
				{
					if( index == 0 || scriptModified[index - 1] != '_' )
						scriptModified = string.Concat( scriptModified.Substring( 0, index ), tokenReplacementsWithTMPChecks[i + 1], scriptModified.Substring( index + tokenReplacementsWithTMPChecks[i].Length ) );
				}
			}

			bool shouldAddExtensionFunctionsToScript = false;
			for( int i = 0; i < tokenReplacementsExtensionFunctions.Length; i += 3 )
			{
				// E.g. "text.alignment = something;" becomes "text.SetTMPAlignment( something );"
				int index = -1;
				while( ( index = scriptModified.IndexOf( tokenReplacementsExtensionFunctions[i], index + 1 ) ) >= 0 )
				{
					int endIndex = scriptModified.IndexOf( ';', index );
					if( endIndex >= 0 )
					{
						int functionParametersStartIndex = index + tokenReplacementsExtensionFunctions[i].Length;
						int functionParametersLength = endIndex - functionParametersStartIndex;

						scriptModified = string.Concat( scriptModified.Substring( 0, index ), tokenReplacementsExtensionFunctions[i + 1],
							scriptModified.Substring( functionParametersStartIndex, functionParametersLength ), tokenReplacementsExtensionFunctions[i + 2], scriptModified.Substring( endIndex ) );

						shouldAddExtensionFunctionsToScript = true;
					}
				}
			}

			for( int i = 0; i < tokenReplacementsAsIs.Length; i += 2 )
				scriptModified = scriptModified.Replace( tokenReplacementsAsIs[i], tokenReplacementsAsIs[i + 1] );

			if( shouldAddExtensionFunctionsToScript )
				AddExtensionFunctionsToScript( scriptPath, ref scriptModified );

			if( scriptModified != script )
			{
				stringBuilder.Append( "Upgrading script: " ).AppendLine( scriptPath );
				File.WriteAllText( scriptPath, scriptModified );
			}
		}

		private void AddExtensionFunctionsToScript( string scriptPath, ref string scriptContents )
		{
			stringBuilder.Append( "Adding TextToTMP extension functions to script: " ).AppendLine( scriptPath );

			int extensionFunctionsInsertIndex = scriptContents.LastIndexOf( "using " );
			if( extensionFunctionsInsertIndex >= 0 )
				extensionFunctionsInsertIndex = scriptContents.IndexOf( '\n', extensionFunctionsInsertIndex ) + 1;
			else
				extensionFunctionsInsertIndex = Mathf.Max( 0, scriptContents.IndexOf( "namespace " ) );

			string namespaceGuid = Guid.NewGuid().ToString().Replace( "-", "" ); // Each namespace should be unique
			scriptContents = scriptContents.Insert( extensionFunctionsInsertIndex, string.Format( EXTENSION_FUNCTIONS_NAMESPACE_DECLARATION, namespaceGuid ) + EXTENSION_FUNCTIONS_BODY );
		}

#if UNITY_2017_3_OR_NEWER
		private void UpgradeAssemblyDefinitionFile( string path )
		{
			if( string.IsNullOrEmpty( path ) || string.IsNullOrEmpty( textMeshProAssemblyDefinitionName ) )
				return;

			if( AssetDatabase.GetMainAssetTypeAtPath( path ) != typeof( UnityEditorInternal.AssemblyDefinitionAsset ) )
				return;

			AssemblyDefinitionFileContents asmFile = JsonUtility.FromJson<AssemblyDefinitionFileContents>( File.ReadAllText( path ) );
			if( asmFile.references != null )
			{
				for( int i = 0; i < asmFile.references.Length; i++ )
				{
#if UNITY_2019_1_OR_NEWER
					string assemblyPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyReference( asmFile.references[i] );
#else
					string assemblyPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName( asmFile.references[i] );
#endif

					if( assemblyPath == textMeshProAssemblyDefinitionFilePath )
						return;
				}
			}

			if( asmFile.references == null )
				asmFile.references = new string[1] { textMeshProAssemblyDefinitionName };
			else
			{
				Array.Resize( ref asmFile.references, asmFile.references.Length + 1 );
				asmFile.references[asmFile.references.Length - 1] = textMeshProAssemblyDefinitionName;
			}

			stringBuilder.Append( "Upgrading Assembly Definition File: " ).AppendLine( path );
			File.WriteAllText( path, JsonUtility.ToJson( asmFile, true ) );
		}
#endif

		private void AddScriptToUpgrade( string scriptPath )
		{
			if( string.IsNullOrEmpty( scriptPath ) )
				return;

			if( !scriptPath.EndsWith( ".cs", StringComparison.OrdinalIgnoreCase ) )
				return;

			if( scriptPath.StartsWith( "Packages/com.unity.textmeshpro", StringComparison.OrdinalIgnoreCase ) || scriptPath.StartsWith( "Packages/com.unity.ui", StringComparison.OrdinalIgnoreCase ) || scriptPath.StartsWith( "Packages/com.unity.ugui", StringComparison.OrdinalIgnoreCase ) )
				return;

			scriptsToUpgrade.Add( scriptPath );
		}
	}
}