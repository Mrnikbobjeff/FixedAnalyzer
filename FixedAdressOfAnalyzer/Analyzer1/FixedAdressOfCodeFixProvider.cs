using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace FixedAdressOfAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FixedAdressOfCodeFixProvider)), Shared]
    public class FixedAdressOfCodeFixProvider : CodeFixProvider
    {
        private const string title = "Use AdressOf Operator";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(FixedAdressOfAnalyzerImp.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<FixedStatementSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedSolution: c => AddAdressOfOperator(context.Document, declaration, c),
                    equivalenceKey: title),
                diagnostic);
        }

        unsafe void x() { var b = new byte[1]; fixed (byte* ptr = b) { } }
        unsafe void y() { var b = new byte[1]; fixed (byte* ptr = &b[0]) { } }

        private async Task<Solution> AddAdressOfOperator(Document document, FixedStatementSyntax fixedStatement, CancellationToken cancellationToken)
        {
            var originalSolution = document.Project.Solution;
            var root = await document.GetSyntaxRootAsync();
            var id = fixedStatement.Declaration.Variables.First().Initializer.Value;
            var argumentSyntax = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)));
            var argList = new SeparatedSyntaxList<ArgumentSyntax>();
            argList = argList.Add(argumentSyntax);
            var elementList = SyntaxFactory.BracketedArgumentList(argList);
            var elemAccess = SyntaxFactory.ElementAccessExpression(id, elementList);
            var adressOfExpression = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.AddressOfExpression, elemAccess);
            var declarator = SyntaxFactory.VariableDeclarator(fixedStatement.Declaration.Variables.First().Identifier, null, SyntaxFactory.EqualsValueClause(adressOfExpression));
            var declaratorList = new SeparatedSyntaxList<VariableDeclaratorSyntax>();
            declaratorList = declaratorList.Add(declarator);
            var Dec = SyntaxFactory.VariableDeclaration(fixedStatement.Declaration.Type, declaratorList);
            root = root.ReplaceNode(fixedStatement.Declaration, Dec);
            var newSolution = originalSolution.WithDocumentSyntaxRoot(document.Id, root);
            return newSolution;
        }
    }
}
