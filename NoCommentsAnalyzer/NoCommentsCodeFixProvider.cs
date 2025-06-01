using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NoCommentsAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NoCommentsCodeFixProvider)), Shared]
    public class NoCommentsCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Remove unauthorized comment";

        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(NoCommentsAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.FirstOrDefault(d => d.Id == NoCommentsAnalyzer.DiagnosticId);
            if (diagnostic == null)
                return;

            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var trivia = root.FindTrivia(diagnosticSpan.Start);

            var action = CodeAction.Create(
                title: Title,
                createChangedDocument: c => RemoveUnauthorizedComment(context.Document, trivia, c),
                equivalenceKey: Title);

            context.RegisterCodeFix(action, diagnostic);
        }

        private static async Task<Document> RemoveUnauthorizedComment(Document document, SyntaxTrivia trivia, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            
            // Remove the comment trivia and any trailing newline to prevent double newlines
            var triviaList = trivia.Token.TrailingTrivia;
            var triviaIndex = triviaList.IndexOf(trivia);
            
            // Check if there's a newline trivia immediately after this comment
            var hasTrailingNewline = triviaIndex >= 0 && triviaIndex < triviaList.Count - 1 && 
                                   triviaList[triviaIndex + 1].IsKind(SyntaxKind.EndOfLineTrivia);
            
            if (hasTrailingNewline)
            {
                // Remove both comment and the trailing newline
                var newTrailingTrivia = triviaList.RemoveAt(triviaIndex).RemoveAt(triviaIndex);
                var newToken = trivia.Token.WithTrailingTrivia(newTrailingTrivia);
                var newRoot = root.ReplaceToken(trivia.Token, newToken);
                return document.WithSyntaxRoot(newRoot);
            }
            else
            {
                // Just remove the comment trivia
                var newRoot = root.ReplaceTrivia(trivia, SyntaxFactory.Whitespace(""));
                return document.WithSyntaxRoot(newRoot);
            }
        }
    }
}