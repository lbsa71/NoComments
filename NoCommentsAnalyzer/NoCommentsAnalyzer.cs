using System.Collections.Immutable;
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
}
