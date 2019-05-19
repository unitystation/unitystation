using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class TestAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor("HONK1001", "Use YieldHelper instead", "Instantiating yield instructions generates garbage, consider using a YieldHelper static instead.", "Garbage", DiagnosticSeverity.Warning, true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(AnalyzeTest, SyntaxKind.YieldReturnStatement);
		}

		static void AnalyzeTest(SyntaxNodeAnalysisContext context)
		{
			// TODO: implement this
			context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
		}
	}
}
