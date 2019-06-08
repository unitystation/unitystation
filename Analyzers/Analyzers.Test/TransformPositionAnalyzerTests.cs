using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Analyzers.Test
{
	[TestClass]
	public class TransformPositionAnalyzerTests : DiagnosticVerifier
	{
		[TestMethod]
		public void Should_Ignore_Other_Access()
		{
			var test = @"
				public class C : B
				{
					private void M()
					{
						System.WriteLine(transform.rotation);
					}
				}
			";

			VerifyCSharpDiagnostic(test);
		}

		[TestMethod]
		public void Should_Detect_Transform_Position_Access()
		{
			var test = @"
				public class C : B
				{
					private void M()
					{
						System.WriteLine(transform.position);
					}
				}
			";

			var expected = new DiagnosticResult
			{
				Id = TransformPositionAnalyzer.DiagnosticId,
				Message = "Do not use transform.position, use registerTile.WorldPositionClient or registerTile.WorldPositionServer.",
				Severity = DiagnosticSeverity.Warning,
				Locations =
					new[] {
							new DiagnosticResultLocation("Test0.cs", 6, 24)
						}
			};

			VerifyCSharpDiagnostic(test, expected);
		}

		[TestMethod]
		public void Should_Detect_Nested_Transform_Position_Access()
		{
			var test = @"
				public class C : B
				{
					private void M()
					{
						something = spriteRenderer.transform.position;
					}
				}
			";

			var expected = new DiagnosticResult
			{
				Id = TransformPositionAnalyzer.DiagnosticId,
				Message = "Do not use transform.position, use registerTile.WorldPositionClient or registerTile.WorldPositionServer.",
				Severity = DiagnosticSeverity.Warning,
				Locations =
					new[] {
							new DiagnosticResultLocation("Test0.cs", 6, 19)
						}
			};

			VerifyCSharpDiagnostic(test, expected);
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new TransformPositionAnalyzer();
		}
	}
}
