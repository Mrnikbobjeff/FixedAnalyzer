using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FixedAdressOfAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class FixedAdressOfAnalyzerImp : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "FixedAdressOfAnalyzer";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Performance";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.FixedStatement);
        }

        static T FindParentOfType<T>(SyntaxNode node) where T : class
        {
            var parent = node.Parent;
            do
            {
                if (parent is T)
                    return parent as T;
                node = parent;
                parent = node.Parent;
            } while (parent != null);
            return null;
        }

        unsafe void x() { var b = new byte[1]; fixed (byte* ptr = b) { } }
        unsafe void y() { var b = new byte[1]; fixed (byte* ptr = &b[0]) { } }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            var fixedStatement = context.Node as FixedStatementSyntax;
            if (fixedStatement.Declaration.Variables.First().Initializer.Value is PrefixUnaryExpressionSyntax prefix && prefix.Kind() == SyntaxKind.AddressOfExpression)
                return; // Already using efficient check.
            //var type = context.SemanticModel.GetTypeInfo(context.Node);
            //var mdec = FindParentOfType<MethodDeclarationSyntax>(context.Node).Body.Statements.FirstOrDefault();
            //var flow = context.SemanticModel.AnalyzeDataFlow(mdec, fixedStatement);
            var diagnostics = Diagnostic.Create(Rule, fixedStatement.GetLocation());
            context.ReportDiagnostic(diagnostics);
        }
    }
}
