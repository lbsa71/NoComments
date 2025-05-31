using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NoCommentsAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NoCommentsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "NC0001";
        private const string Category = "Formatting";
        private const string HelpLinkUri = "https://github.com/lbsa71/no-ai-comments";

        private const string Shibboleth = "[!]";
        
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            title: "Unauthorized comment detected",
            messageFormat: "Comments must include the shibboleth string '[!]' to verify they are human-written",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "All non-XML documentation comments must include the '[!]' marker to indicate they are human-written.",
            helpLinkUri: HelpLinkUri);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
        }

        private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var root = context.Tree.GetRoot(context.CancellationToken);
            var trivia = root.DescendantTrivia();

            foreach (var t in trivia)
            {
                // Check only single-line and multi-line comments (not XML doc comments)
                if ((t.IsKind(SyntaxKind.SingleLineCommentTrivia) || 
                     t.IsKind(SyntaxKind.MultiLineCommentTrivia)) && 
                    !t.ToString().Contains(Shibboleth))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, t.GetLocation()));
                }
                
                /* [!] We don't check XML documentation comments (SyntaxKind.SingleLineDocumentationCommentTrivia 
                   or SyntaxKind.MultiLineDocumentationCommentTrivia) as per requirements */
            }
        }
    }

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NoCommentsCodeFixProvider)), Shared]
    public class NoCommentsCodeFixProvider : CodeFixProvider
    {
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

            // Find the comment trivia at the diagnostic location
            var token = root.FindToken(diagnosticSpan.Start);
            var commentTrivia = token.LeadingTrivia.Concat(token.TrailingTrivia)
                .FirstOrDefault(t => t.Span.IntersectsWith(diagnosticSpan) && 
                               (t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia)));

            if (commentTrivia.IsKind(SyntaxKind.None))
            {
                // Try to find it in the previous token's trailing trivia
                var previousToken = token.GetPreviousToken();
                commentTrivia = previousToken.TrailingTrivia
                    .FirstOrDefault(t => t.Span.IntersectsWith(diagnosticSpan) && 
                                   (t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia)));
            }

            if (!commentTrivia.IsKind(SyntaxKind.None))
            {
                var action = CodeAction.Create(
                    title: "Remove unauthorized comment",
                    createChangedDocument: c => RemoveCommentAsync(context.Document, commentTrivia, c),
                    equivalenceKey: "RemoveUnauthorizedComment");

                context.RegisterCodeFix(action, diagnostic);
            }
        }

        private static async Task<Document> RemoveCommentAsync(Document document, SyntaxTrivia commentTrivia, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            
            // Find the token that contains this trivia
            var token = root.FindToken(commentTrivia.SpanStart);
            
            // Remove the comment trivia
            SyntaxToken newToken;
            if (token.LeadingTrivia.Contains(commentTrivia))
            {
                var newLeadingTrivia = token.LeadingTrivia.Remove(commentTrivia);
                newToken = token.WithLeadingTrivia(newLeadingTrivia);
            }
            else if (token.TrailingTrivia.Contains(commentTrivia))
            {
                var newTrailingTrivia = token.TrailingTrivia.Remove(commentTrivia);
                newToken = token.WithTrailingTrivia(newTrailingTrivia);
            }
            else
            {
                // Try the previous token's trailing trivia
                var previousToken = token.GetPreviousToken();
                if (previousToken.TrailingTrivia.Contains(commentTrivia))
                {
                    var newTrailingTrivia = previousToken.TrailingTrivia.Remove(commentTrivia);
                    var newPreviousToken = previousToken.WithTrailingTrivia(newTrailingTrivia);
                    var newRoot = root.ReplaceToken(previousToken, newPreviousToken);
                    return document.WithSyntaxRoot(newRoot);
                }
                return document; // Could not find the trivia
            }

            var updatedRoot = root.ReplaceToken(token, newToken);
            return document.WithSyntaxRoot(updatedRoot);
        }
    }
}
