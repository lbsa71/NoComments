using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
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

        // Intentional comment markers
        private static readonly string[] IntentionalMarkers = { "HUMAN:", "NOTE:", "INTENT:", "OK:", "[!]" };
        
        // TODO/suppression patterns
        private static readonly string[] SuppressionPatterns = { "TODO:", "HACK:", "FIXME:" };
        
        // License/banner patterns
        private static readonly string[] LicensePatterns = { "Copyright", "Licensed", "SPDX-License-Identifier" };
        
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            title: "Unauthorized comment detected",
            messageFormat: "Comments must use intentional markers (HUMAN:, NOTE:, INTENT:, OK:) or be TODO/HACK/FIXME patterns, or file-level license banners",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Comments must be explicitly marked as intentional using approved markers, be TODO/HACK/FIXME patterns, file-level license banners, or XML documentation.",
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
            var trivia = root.DescendantTrivia().ToList();

            for (int i = 0; i < trivia.Count; i++)
            {
                var t = trivia[i];
                
                // Check only single-line and multi-line comments (not XML doc comments)
                if (!(t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia)))
                    continue;

                var commentText = t.ToString();
                
                // Skip if it contains intentional markers
                if (ContainsIntentionalMarker(commentText))
                    continue;
                    
                // Skip if it's a TODO/HACK/FIXME pattern
                if (IsSuppressionPattern(commentText))
                    continue;
                    
                // Skip if it's a file-level banner comment
                if (IsFileLevelBanner(t, root))
                    continue;

                // If we get here, it's an unauthorized comment
                context.ReportDiagnostic(Diagnostic.Create(Rule, t.GetLocation()));
            }
        }

        private static bool ContainsIntentionalMarker(string commentText)
        {
            return IntentionalMarkers.Any(marker => commentText.Contains(marker));
        }

        private static bool IsSuppressionPattern(string commentText)
        {
            var trimmed = commentText.TrimStart('/', '*', ' ', '\t');
            return SuppressionPatterns.Any(pattern => trimmed.StartsWith(pattern, System.StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsFileLevelBanner(SyntaxTrivia trivia, SyntaxNode root)
        {
            // Check if this is at the very top of the file (before any namespace or using directives)
            var triviaPosition = trivia.SpanStart;
            
            // Find the first meaningful syntax node (namespace, using, class, etc.)
            var firstMeaningfulNode = root.DescendantNodes().FirstOrDefault(n => 
                n.IsKind(SyntaxKind.NamespaceDeclaration) ||
                n.IsKind(SyntaxKind.UsingDirective) ||
                n.IsKind(SyntaxKind.ClassDeclaration) ||
                n.IsKind(SyntaxKind.InterfaceDeclaration) ||
                n.IsKind(SyntaxKind.StructDeclaration) ||
                n.IsKind(SyntaxKind.EnumDeclaration));

            // If no meaningful node found, consider it top-level
            if (firstMeaningfulNode == null)
                return CheckLicenseContent(trivia.ToString());

            // Comment must appear before the first meaningful node
            if (triviaPosition >= firstMeaningfulNode.SpanStart)
                return false;

            return CheckLicenseContent(trivia.ToString());
        }

        private static bool CheckLicenseContent(string commentText)
        {
            return LicensePatterns.Any(pattern => commentText.IndexOf(pattern, System.StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
