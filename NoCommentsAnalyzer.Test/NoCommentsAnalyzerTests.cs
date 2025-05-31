using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Immutable;

namespace NoCommentsAnalyzer.Test
{
    [TestClass]
    public class NoCommentsAnalyzerTests
    {
        [TestMethod]
        public void TestAnalyzerIsCreatedSuccessfully()
        {
            // This test simply verifies that the analyzer can be instantiated
            var analyzer = new NoCommentsAnalyzer();
            Assert.IsNotNull(analyzer);
            
            // Verify diagnostic ID
            Assert.AreEqual("NC0001", NoCommentsAnalyzer.DiagnosticId);
            
            // Verify we have 1 supported diagnostic
            Assert.AreEqual(1, analyzer.SupportedDiagnostics.Length);
        }

        [TestMethod]
        public void TestCodeFixProviderIsCreatedSuccessfully()
        {
            // This test simply verifies that the code fix provider can be instantiated
            var codeFixProvider = new NoCommentsCodeFixProvider();
            Assert.IsNotNull(codeFixProvider);
            
            // Verify it can fix NC0001
            Assert.IsTrue(codeFixProvider.FixableDiagnosticIds.Contains("NC0001"));
        }
    }
}
