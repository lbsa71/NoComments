using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NoCommentsAnalyzer
{
    /// <summary>
    /// Configuration options for the NoCommentsAnalyzer
    /// </summary>
    public class AnalyzerConfiguration
    {
        public bool EnableIntentionalMarkersCheck { get; set; } = true;
        public bool EnableSuppressionPatternsCheck { get; set; } = true;
        public bool EnableLicenseBannerCheck { get; set; } = true;
        public bool EnableXmlDocumentationCheck { get; set; } = true;
        
        public string[] IntentionalMarkers { get; set; } = { "HUMAN:", "NOTE:", "INTENT:", "OK:", "[!]" };
        public string[] SuppressionPatterns { get; set; } = { "TODO:", "HACK:", "FIXME:" };
        public string[] LicensePatterns { get; set; } = { "Copyright", "Licensed", "SPDX-License-Identifier" };
        
        public bool IsDisabledForFile { get; set; } = false;
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NoCommentsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "NC0001";
        private const string Category = "Formatting";
        private const string HelpLinkUri = "https://github.com/lbsa71/no-ai-comments";
        
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
            
            // Read configuration from .editorconfig
            var config = ReadConfiguration(context.Options, context.Tree);
            
            // Check if analyzer is disabled for this file
            if (config.IsDisabledForFile)
                return;
                
            var trivia = root.DescendantTrivia().ToList();

            for (int i = 0; i < trivia.Count; i++)
            {
                var t = trivia[i];
                
                // Check only single-line and multi-line comments (not XML doc comments)
                if (!(t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia)))
                    continue;

                var commentText = t.ToString();
                
                // Check for inline disabling - skip if comment contains disable directive
                if (IsInlineDisabled(commentText))
                    continue;
                
                // Skip if intentional markers check is enabled and comment contains markers
                if (config.EnableIntentionalMarkersCheck && ContainsIntentionalMarker(commentText, config.IntentionalMarkers))
                    continue;
                    
                // Skip if suppression patterns check is enabled and comment matches patterns
                if (config.EnableSuppressionPatternsCheck && IsSuppressionPattern(commentText, config.SuppressionPatterns))
                    continue;
                    
                // Skip if license banner check is enabled and comment is a file-level banner
                if (config.EnableLicenseBannerCheck && IsFileLevelBanner(t, root, config.LicensePatterns))
                    continue;

                // If we get here, it's an unauthorized comment
                context.ReportDiagnostic(Diagnostic.Create(Rule, t.GetLocation()));
            }
        }

        private static AnalyzerConfiguration ReadConfiguration(AnalyzerOptions options, SyntaxTree tree)
        {
            var config = new AnalyzerConfiguration();
            
            var configOptions = options.AnalyzerConfigOptionsProvider.GetOptions(tree);
            
            // Read feature enable/disable flags
            if (configOptions.TryGetValue("nocomments_analyzer.enable_intentional_markers", out var enableIntentional))
                config.EnableIntentionalMarkersCheck = !bool.TryParse(enableIntentional, out var result1) || result1;
                
            if (configOptions.TryGetValue("nocomments_analyzer.enable_suppression_patterns", out var enableSuppression))
                config.EnableSuppressionPatternsCheck = !bool.TryParse(enableSuppression, out var result2) || result2;
                
            if (configOptions.TryGetValue("nocomments_analyzer.enable_license_banner", out var enableLicense))
                config.EnableLicenseBannerCheck = !bool.TryParse(enableLicense, out var result3) || result3;
                
            if (configOptions.TryGetValue("nocomments_analyzer.enable_xml_documentation", out var enableXml))
                config.EnableXmlDocumentationCheck = !bool.TryParse(enableXml, out var result4) || result4;
                
            // Read file-level disable flag
            if (configOptions.TryGetValue("nocomments_analyzer.disable_for_file", out var disableFile))
                config.IsDisabledForFile = bool.TryParse(disableFile, out var result5) && result5;
            
            // Read custom patterns/markers - only override if explicitly set
            if (configOptions.TryGetValue("nocomments_analyzer.intentional_markers", out var customIntentional) && !string.IsNullOrWhiteSpace(customIntentional))
                config.IntentionalMarkers = customIntentional.Split(',').Select(m => m.Trim()).Where(m => !string.IsNullOrEmpty(m)).ToArray();
                
            if (configOptions.TryGetValue("nocomments_analyzer.suppression_patterns", out var customSuppression) && !string.IsNullOrWhiteSpace(customSuppression))
                config.SuppressionPatterns = customSuppression.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToArray();
                
            if (configOptions.TryGetValue("nocomments_analyzer.license_patterns", out var customLicense) && !string.IsNullOrWhiteSpace(customLicense))
                config.LicensePatterns = customLicense.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToArray();
            
            return config;
        }

        private static bool IsInlineDisabled(string commentText)
        {
            // Check for inline disable directives like // nocomments:disable
            var trimmed = commentText.TrimStart('/', '*', ' ', '\t');
            return trimmed.StartsWith("nocomments:disable", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsIntentionalMarker(string commentText, string[] markers)
        {
            return markers.Any(marker => commentText.Contains(marker));
        }

        private static bool IsSuppressionPattern(string commentText, string[] patterns)
        {
            var trimmed = commentText.TrimStart('/', '*', ' ', '\t');
            return patterns.Any(pattern => trimmed.StartsWith(pattern, System.StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsFileLevelBanner(SyntaxTrivia trivia, SyntaxNode root, string[] licensePatterns)
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
                return CheckLicenseContent(trivia.ToString(), licensePatterns);

            // Comment must appear before the first meaningful node
            if (triviaPosition >= firstMeaningfulNode.SpanStart)
                return false;

            // Find the contiguous block of comments that this trivia belongs to
            // and check if any comment in that block contains license patterns
            return IsPartOfLicenseBlock(trivia, root, firstMeaningfulNode, licensePatterns);
        }

        private static bool CheckLicenseContent(string commentText, string[] licensePatterns)
        {
            return licensePatterns.Any(pattern => commentText.IndexOf(pattern, System.StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool IsPartOfLicenseBlock(SyntaxTrivia trivia, SyntaxNode root, SyntaxNode firstMeaningfulNode, string[] licensePatterns)
        {
            // Get all trivia before the first meaningful node
            var allTrivia = root.DescendantTrivia().Where(t => t.SpanStart < firstMeaningfulNode.SpanStart).ToList();
            
            // Find contiguous blocks of comments
            var commentBlocks = GetContiguousCommentBlocks(allTrivia);
            
            // Find which block contains our trivia
            var containingBlock = commentBlocks.FirstOrDefault(block => block.Contains(trivia));
            
            if (containingBlock == null)
                return false;
            
            // Check if any comment in the containing block has license content
            return containingBlock.Any(t => CheckLicenseContent(t.ToString(), licensePatterns));
        }

        private static List<List<SyntaxTrivia>> GetContiguousCommentBlocks(List<SyntaxTrivia> allTrivia)
        {
            var blocks = new List<List<SyntaxTrivia>>();
            var currentBlock = new List<SyntaxTrivia>();
            
            foreach (var trivia in allTrivia)
            {
                if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                {
                    currentBlock.Add(trivia);
                }
                else if (trivia.IsKind(SyntaxKind.EndOfLineTrivia) || trivia.IsKind(SyntaxKind.WhitespaceTrivia))
                {
                    // Whitespace and newlines don't break comment blocks
                    continue;
                }
                else
                {
                    // Other trivia types break comment blocks
                    if (currentBlock.Count > 0)
                    {
                        blocks.Add(new List<SyntaxTrivia>(currentBlock));
                        currentBlock.Clear();
                    }
                }
            }
            
            // Add the last block if it has comments
            if (currentBlock.Count > 0)
            {
                blocks.Add(currentBlock);
            }
            
            return blocks;
        }
    }
}
