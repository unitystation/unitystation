using Analyzers.Extensions;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class TransformPositionAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "HONK1002";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			DiagnosticId,
			"Avoid transform.position",
			"Do not use transform.position, use registerTile.WorldPositionClient or registerTile.WorldPositionServer.",
			"Code Review",
			DiagnosticSeverity.Warning,
			helpLinkUri: "HONK1002_AvoidTransformPosition.md",
			isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.SimpleMemberAccessExpression);
		}

		public static void AnalyzeNode(SyntaxNodeAnalysisContext context)
		{
			var accessSyntax = (MemberAccessExpressionSyntax)context.Node;
			var (identifierNode, memberNode) = accessSyntax.ChildNodes();
			// transform.position
			//           ^      ^
			if (memberNode is IdentifierNameSyntax memberSyntax &&
				memberSyntax.Identifier.Text == "position" &&
				HasTransformIdentifier(identifierNode))
			{
				var diagnostic = Diagnostic.Create(Rule, accessSyntax.GetLocation());
				context.ReportDiagnostic(diagnostic);
			}
		}

		private static bool HasTransformIdentifier(SyntaxNode identifierNode)
		{
			// transform.position
			// ^       ^
			if (identifierNode is IdentifierNameSyntax identifierSyntax &&
				identifierSyntax.Identifier.Text == "transform")
			{
				return true;
			}

			// foo.transform.position
			// ^           ^
			if (identifierNode is MemberAccessExpressionSyntax accessSyntax &&
				accessSyntax.Kind() == SyntaxKind.SimpleMemberAccessExpression)
			{
				var (_, memberNode) = accessSyntax.ChildNodes();
				return memberNode is IdentifierNameSyntax memberSyntax && memberSyntax.Identifier.Text == "transform";
			}

			return false;
		}
	}
}
