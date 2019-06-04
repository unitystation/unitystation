using Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class WaitForAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "HONK1001";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			DiagnosticId,
			"Use WaitFor",
			"Instantiating yield instructions generates garbage, consider using WaitFor.Seconds() instead.", "Performance",
			DiagnosticSeverity.Warning,
			helpLinkUri: "HONK1001_UseWaitFor.md",
			isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			// TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
			// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
			context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.YieldReturnStatement);
		}

		private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
		{
			var yieldSyntax = (YieldStatementSyntax)context.Node;

			var createSyntax = yieldSyntax.ChildNodes().FirstOrDefault() as ObjectCreationExpressionSyntax;
			if (createSyntax is null) return;

			var (identifierNode, argsNode) = createSyntax.ChildNodes();
			if (identifierNode is IdentifierNameSyntax identifierSyntax &&
				identifierSyntax.Identifier.Text == "WaitForSeconds" &&
				argsNode is ArgumentListSyntax argsSyntax &&
				argsSyntax.ChildNodes().FirstOrDefault() is ArgumentSyntax paramSyntax &&
				paramSyntax.Expression is LiteralExpressionSyntax)
			{
				var diagnostic = Diagnostic.Create(Rule, yieldSyntax.GetLocation());
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
