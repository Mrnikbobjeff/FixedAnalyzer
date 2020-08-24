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
using System;

namespace FixedAdressOfAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FixedAdressOfCodeFixProvider)), Shared]
    public class FixedAdressOfCodeFixProvider : CodeFixProvider
    {
        const string title = "Use AdressOf Operator";

        readonly Lazy<BracketedArgumentListSyntax> zeroAccessor = new Lazy<BracketedArgumentListSyntax>(() => CreateZeroIndex());
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(FixedAdressOfAnalyzerImp.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            foreach(var diagnostic in context.Diagnostics)
            {
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
        }

        static BracketedArgumentListSyntax CreateZeroIndex()
        {
            var argumentSyntax = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)));
            var argList = new SeparatedSyntaxList<ArgumentSyntax>();
            argList = argList.Add(argumentSyntax);
            var elementList = SyntaxFactory.BracketedArgumentList(argList);
            return elementList;
        }

        VariableDeclaratorSyntax CreateDeclarator(VariableDeclaratorSyntax variable)
        {
            var elementList = zeroAccessor.Value;
            var elemAccess = SyntaxFactory.ElementAccessExpression(variable.Initializer.Value, elementList);
            var adressOfExpression = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.AddressOfExpression, elemAccess);
            return SyntaxFactory.VariableDeclarator(variable.Identifier, null, SyntaxFactory.EqualsValueClause(adressOfExpression));
        }

        async Task<Solution> AddAdressOfOperator(Document document, FixedStatementSyntax fixedStatement, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
            var declaratorList = new SeparatedSyntaxList<VariableDeclaratorSyntax>();
            foreach (var variable in fixedStatement.Declaration.Variables)
            {
                var declarator = CreateDeclarator(variable);
                declaratorList = declaratorList.Add(declarator);
            }
            var fixedDeclaration = SyntaxFactory.VariableDeclaration(fixedStatement.Declaration.Type, declaratorList);
            root = root.ReplaceNode(fixedStatement.Declaration, fixedDeclaration);
            return document.Project.Solution.WithDocumentSyntaxRoot(document.Id, root);
        }
    }
}
