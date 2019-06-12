using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Analyzers.Test
{
    [TestClass]
    public class WaitForAnalyzerTests : DiagnosticVerifier
    {
        [TestMethod]
        public void Should_Ignore_Non_Static_Duration()
        {
            var test = @"
				public class FireCabinetTrigger : InputTrigger
				{
					private IEnumerator WaitForLoad()
					{
						yield return new WaitForSeconds(Math.Random());
						SyncCabinet(IsClosed);
						SyncItemSprite(isFull);
					}					
				}
			";

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
                Id = WaitForAnalyzer.DiagnosticId,
                Message = "Instantiating yield instructions generates garbage, consider using WaitFor.Seconds() instead.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 6, 7)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new WaitForAnalyzer();
        }
    }
}
