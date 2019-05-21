using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class YieldHelperAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "HONK1001";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, "Use YieldHelper", "Instantiating yield instructions generates garbage, consider using a YieldHelper static instead.", "Performance", DiagnosticSeverity.Warning, isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			// TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
			// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
			context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.YieldReturnStatement);
		}

		private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
		{
			var syntaxNode = (YieldStatementSyntax)context.Node;
			var diagnostic = Diagnostic.Create(Rule, syntaxNode.GetLocation());
			context.ReportDiagnostic(diagnostic);
		}
	}
}
