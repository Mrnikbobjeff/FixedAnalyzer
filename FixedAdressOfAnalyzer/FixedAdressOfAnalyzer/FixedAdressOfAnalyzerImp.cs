using System.Collections.Immutable;
using System.Linq;
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

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            var fixedStatement = context.Node as FixedStatementSyntax;
            if (fixedStatement.Declaration.Variables.All(x => x.Initializer.Value is PrefixUnaryExpressionSyntax prefix && prefix.Kind() == SyntaxKind.AddressOfExpression))
                return; // Already using efficient check.
            var diagnosticsToCreate = fixedStatement.Declaration.Variables.Where(x => !(x.Initializer.Value is PrefixUnaryExpressionSyntax prefix && prefix.Kind() == SyntaxKind.AddressOfExpression) && context.SemanticModel.GetTypeInfo(x.Initializer.Value).Type.TypeKind == TypeKind.Array); //Last check may be redundant, non Array types are always prefixed with AdressOfOperator
                var diagnostics = diagnosticsToCreate.Select(dec => Diagnostic.Create(Rule, dec.GetLocation()));
            foreach(var d in diagnostics)
            {
                context.ReportDiagnostic(d);
            }
        }
    }
}
