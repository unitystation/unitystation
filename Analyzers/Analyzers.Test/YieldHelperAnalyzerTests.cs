using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Analyzers.Test
{
	[TestClass]
	public class YieldHelperAnalyzerTests : DiagnosticVerifier
	{
		[TestMethod]
		public void Should_Ignore()
		{
			var test = @"";

			VerifyCSharpDiagnostic(test);
		}

		[TestMethod]
		public void Should_Warn()
		{
			var test = @"
				public class FireCabinetTrigger : InputTrigger
				{
					private IEnumerator WaitForLoad()
					{
						yield return new WaitForSeconds(3f);
						SyncCabinet(IsClosed);
						SyncItemSprite(isFull);
					}					
				}
			";

			var expected = new DiagnosticResult
			{
				Id = YieldHelperAnalyzer.DiagnosticId,
				Severity = DiagnosticSeverity.Warning,
			};

			VerifyCSharpDiagnostic(test, expected);
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new YieldHelperAnalyzer();
		}
	}
}
