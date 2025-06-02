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
        private const string RemoveTitle = "Remove unauthorized comment";
        private const string KeepTitle = "Keep comment with intentional marker";
        private const string NormalizeTitle = "Normalize suppression pattern";

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
            var commentText = trivia.ToString();

            // Check if this comment could be normalized as a suppression pattern
            if (IsNormalizableSuppressionPattern(commentText))
            {
                // Register the normalize action first (preferred option)
                var normalizeAction = CodeAction.Create(
                    title: NormalizeTitle,
                    createChangedDocument: c => NormalizeSuppressionPattern(context.Document, trivia, c),
                    equivalenceKey: NormalizeTitle);

                context.RegisterCodeFix(normalizeAction, diagnostic);
            }

            // Register the remove action
            var removeAction = CodeAction.Create(
                title: RemoveTitle,
                createChangedDocument: c => RemoveUnauthorizedComment(context.Document, trivia, c),
                equivalenceKey: RemoveTitle);

            context.RegisterCodeFix(removeAction, diagnostic);

            // Register the keep action
            var keepAction = CodeAction.Create(
                title: KeepTitle,
                createChangedDocument: c => KeepCommentWithMarker(context.Document, trivia, c),
                equivalenceKey: KeepTitle);

            context.RegisterCodeFix(keepAction, diagnostic);
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

        private static async Task<Document> KeepCommentWithMarker(Document document, SyntaxTrivia trivia, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            
            // Read configuration to get the intentional marker to use
            var marker = GetIntentionalMarker(document);
            
            // Modify the comment to include the intentional marker
            var originalText = trivia.ToString();
            var newCommentText = AddIntentionalMarker(originalText, marker);
            
            // Create new trivia with the modified text
            var newTrivia = trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                ? SyntaxFactory.Comment(newCommentText)
                : SyntaxFactory.Comment(newCommentText);
            
            var newRoot = root.ReplaceTrivia(trivia, newTrivia);
            return document.WithSyntaxRoot(newRoot);
        }

        private static string GetIntentionalMarker(Document document)
        {
            // Use "NOTE:" as the default marker as specified in the issue
            // This ensures the marker is one of the allowed strings from the default configuration
            // In the future, this could be enhanced to read from configuration if needed
            return "NOTE:";
        }

        private static string AddIntentionalMarker(string originalComment, string marker)
        {
            if (originalComment.StartsWith("//"))
            {
                // Single-line comment: // comment -> // NOTE: comment
                var commentContent = originalComment.Substring(2).Trim();
                return $"// {marker} {commentContent}";
            }
            else if (originalComment.StartsWith("/*") && originalComment.EndsWith("*/"))
            {
                // Multi-line comment: /* comment */ -> /* NOTE: comment */
                var commentContent = originalComment.Substring(2, originalComment.Length - 4).Trim();
                return $"/* {marker} {commentContent} */";
            }
            
            // Fallback: just prepend the marker
            return $"{marker} {originalComment}";
        }

        private static bool IsNormalizableSuppressionPattern(string commentText)
        {
            var defaultPatterns = new[] { "TODO:", "HACK:", "FIXME:" };
            var trimmed = commentText.TrimStart('/', '*', ' ', '\t');
            
            return defaultPatterns.Any(pattern => 
            {
                var basePattern = pattern.TrimEnd(':');
                if (trimmed.StartsWith(basePattern, System.StringComparison.OrdinalIgnoreCase))
                {
                    var nextCharIndex = basePattern.Length;
                    if (nextCharIndex >= trimmed.Length)
                        return false; // End of string after pattern - needs punctuation
                        
                    var nextChar = trimmed[nextCharIndex];
                    // Return true if it uses non-colon punctuation (needs normalization)
                    return nextChar == ';' || nextChar == ',' || nextChar == '-';
                }
                return false;
            });
        }

        private static async Task<Document> NormalizeSuppressionPattern(Document document, SyntaxTrivia trivia, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            
            var originalText = trivia.ToString();
            var normalizedText = NormalizeSuppressionPatternText(originalText);
            
            // Create new trivia with the normalized text
            var newTrivia = trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                ? SyntaxFactory.Comment(normalizedText)
                : SyntaxFactory.Comment(normalizedText);
            
            var newRoot = root.ReplaceTrivia(trivia, newTrivia);
            return document.WithSyntaxRoot(newRoot);
        }

        private static string NormalizeSuppressionPatternText(string commentText)
        {
            var defaultPatterns = new[] { "TODO:", "HACK:", "FIXME:" };
            
            if (commentText.StartsWith("//"))
            {
                var content = commentText.Substring(2).TrimStart();
                foreach (var pattern in defaultPatterns)
                {
                    var basePattern = pattern.TrimEnd(':');
                    if (content.StartsWith(basePattern, System.StringComparison.OrdinalIgnoreCase))
                    {
                        var nextCharIndex = basePattern.Length;
                        if (nextCharIndex < content.Length)
                        {
                            var nextChar = content[nextCharIndex];
                            if (nextChar == ';' || nextChar == ',' || nextChar == '-')
                            {
                                // Replace the punctuation with colon
                                var remaining = content.Substring(nextCharIndex + 1);
                                return $"// {basePattern.ToUpper()}: {remaining.TrimStart()}";
                            }
                        }
                    }
                }
            }
            else if (commentText.StartsWith("/*") && commentText.EndsWith("*/"))
            {
                var content = commentText.Substring(2, commentText.Length - 4).Trim();
                foreach (var pattern in defaultPatterns)
                {
                    var basePattern = pattern.TrimEnd(':');
                    if (content.StartsWith(basePattern, System.StringComparison.OrdinalIgnoreCase))
                    {
                        var nextCharIndex = basePattern.Length;
                        if (nextCharIndex < content.Length)
                        {
                            var nextChar = content[nextCharIndex];
                            if (nextChar == ';' || nextChar == ',' || nextChar == '-')
                            {
                                // Replace the punctuation with colon
                                var remaining = content.Substring(nextCharIndex + 1).Trim();
                                return $"/* {basePattern.ToUpper()}: {remaining} */";
                            }
                        }
                    }
                }
            }
            
            return commentText; // Return unchanged if no pattern matched
        }
    }
}