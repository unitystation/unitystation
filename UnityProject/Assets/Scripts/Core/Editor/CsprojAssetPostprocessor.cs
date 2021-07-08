using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace RoslynAnalyserSupport
{
	///Roslyn analyzer support for Rider+Unity. 
	///Press "Assets-Sync C# Project"
	public class CsprojAssetPostprocessor : AssetPostprocessor
	{
		public override int GetPostprocessOrder()
		{
			return 20;
		}

		private static string[] GetCsprojLinesInSln()
		{
			var projectDirectory = Directory.GetParent( Application.dataPath ).FullName;
			var projectName = Path.GetFileName( projectDirectory );
			var slnFile = Path.GetFullPath( string.Format( "{0}.sln", projectName ) );
			if ( !File.Exists( slnFile ) )
			{
				return new string[0];
			}

			var slnAllText = File.ReadAllText( slnFile );
			var lines = slnAllText.Split( new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries )
				.Where( a => a.StartsWith( "Project(" ) ).ToArray();
			return lines;
		}

		public static void OnGeneratedCSProjectFiles()
		{
			try
			{
				// get only csproj files, which are mentioned in sln
				var lines = GetCsprojLinesInSln();
				var currentDirectory = Directory.GetCurrentDirectory();
				var projectFiles = Directory.GetFiles( currentDirectory, "*.csproj" )
					.Where( csprojFile => lines.Any( line => line.Contains( "\"" + Path.GetFileName( csprojFile ) + "\"" ) ) ).ToArray();

				foreach ( var file in projectFiles )
				{
					UpgradeProjectFile( file );
				}
			} catch ( Exception e )
			{
				// unhandled exception kills editor
				Debug.LogError( e );
			}
		}

		private static void UpgradeProjectFile( string projectFile )
		{
			XDocument doc;
			try
			{
				doc = XDocument.Load( projectFile );
			} catch ( Exception )
			{
				Debug.LogError( string.Format( "Failed to Load {0}", projectFile ) );
				return;
			}

			var projectContentElement = doc.Root;
			XNamespace xmlns = projectContentElement.Name.NamespaceName; // do not use var
			SetRoslynAnalyzers( projectContentElement, xmlns );

			doc.Save( projectFile );
		}

		// add everything from lib folder to csproj
		//<ItemGroup><Analyzer Include="lib\UnityEngineAnalyzer.1.0.0.0\analyzers\dotnet\cs\UnityEngineAnalyzer.dll" /></ItemGroup>
		//<CodeAnalysisRuleSet>..\path\to\myrules.ruleset</CodeAnalysisRuleSet>
		private static void SetRoslynAnalyzers( XElement projectContentElement, XNamespace xmlns )
		{
			var currentDirectory = Directory.GetCurrentDirectory();
			var roslynAnalysersBaseDir = new DirectoryInfo( Path.Combine( currentDirectory, "lib" ) );
			if ( !roslynAnalysersBaseDir.Exists )
			{
				return;
			}

			var relPaths = roslynAnalysersBaseDir.GetFiles( "*", SearchOption.AllDirectories )
				.Select( x => x.FullName.Substring( currentDirectory.Length + 1 ) );
			var itemGroup = new XElement( xmlns + "ItemGroup" );
			foreach ( var file in relPaths )
			{
				if ( new FileInfo( file ).Extension == ".dll" )
				{
					var reference = new XElement( xmlns + "Analyzer" );
					reference.Add( new XAttribute( "Include", file ) );
					itemGroup.Add( reference );
				}

				if ( new FileInfo( file ).Extension == ".ruleset" )
				{
					SetOrUpdateProperty( projectContentElement, xmlns, "CodeAnalysisRuleSet", existing => file );
				}
			}

			projectContentElement.Add( itemGroup );
		}

		private static bool SetOrUpdateProperty( XElement root, XNamespace xmlns, string name, Func<string, string> updater )
		{
			var element = root.Elements( xmlns + "PropertyGroup" ).Elements( xmlns + name ).FirstOrDefault();
			if ( element != null )
			{
				var result = updater( element.Value );
				if ( result != element.Value )
				{
					Debug.Log( string.Format( "Overriding existing project property {0}. Old value: {1}, new value: {2}", name,
						element.Value, result ) );

					element.SetValue( result );
					return true;
				}

				Debug.Log( string.Format( "Property {0} already set. Old value: {1}, new value: {2}", name, element.Value, result ) );
			} else
			{
				AddProperty( root, xmlns, name, updater( string.Empty ) );
				return true;
			}

			return false;
		}

		// Adds a property to the first property group without a condition
		private static void AddProperty( XElement root, XNamespace xmlns, string name, string content )
		{
			Debug.Log( string.Format( "Adding project property {0}. Value: {1}", name, content ) );

			var propertyGroup = root.Elements( xmlns + "PropertyGroup" )
				.FirstOrDefault( e => !e.Attributes( xmlns + "Condition" ).Any() );
			if ( propertyGroup == null )
			{
				propertyGroup = new XElement( xmlns + "PropertyGroup" );
				root.AddFirst( propertyGroup );
			}

			propertyGroup.Add( new XElement( xmlns + name, content ) );
		}
	}
}
