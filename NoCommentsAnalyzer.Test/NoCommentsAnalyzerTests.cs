using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

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

        [TestMethod]
        public void TestDefaultConfigurationValues()
        {
            // Test that default configuration has all features enabled
            var config = new AnalyzerConfiguration();
            
            Assert.IsTrue(config.EnableIntentionalMarkersCheck);
            Assert.IsTrue(config.EnableSuppressionPatternsCheck);
            Assert.IsTrue(config.EnableLicenseBannerCheck);
            Assert.IsTrue(config.EnableXmlDocumentationCheck);
            Assert.IsFalse(config.IsDisabledForFile);
            
            // Test default marker arrays
            Assert.IsTrue(config.IntentionalMarkers.Contains("HUMAN:"));
            Assert.IsTrue(config.IntentionalMarkers.Contains("NOTE:"));
            Assert.IsTrue(config.IntentionalMarkers.Contains("INTENT:"));
            Assert.IsTrue(config.IntentionalMarkers.Contains("OK:"));
            Assert.IsTrue(config.IntentionalMarkers.Contains("[!]"));
            
            Assert.IsTrue(config.SuppressionPatterns.Contains("TODO:"));
            Assert.IsTrue(config.SuppressionPatterns.Contains("HACK:"));
            Assert.IsTrue(config.SuppressionPatterns.Contains("FIXME:"));
            
            Assert.IsTrue(config.LicensePatterns.Contains("Copyright"));
            Assert.IsTrue(config.LicensePatterns.Contains("Licensed"));
            Assert.IsTrue(config.LicensePatterns.Contains("SPDX-License-Identifier"));
        }

        [TestMethod]
        public void TestInlineDisablingDetection()
        {
            // Test inline disabling functionality
            var analyzer = new NoCommentsAnalyzer();
            
            // Test various inline disable formats
            var testCases = new[]
            {
                "// nocomments:disable This should be ignored",
                "/* nocomments:disable This should be ignored */",
                "//   nocomments:disable   This should be ignored",
                "//\tnocomments:disable\tThis should be ignored"
            };
            
            foreach (var testCase in testCases)
            {
                // Using reflection to access private method for testing
                var isInlineDisabledMethod = typeof(NoCommentsAnalyzer)
                    .GetMethod("IsInlineDisabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                
                var result = (bool)isInlineDisabledMethod.Invoke(null, new object[] { testCase });
                Assert.IsTrue(result, $"Failed to detect inline disable in: {testCase}");
            }
            
            // Test that regular comments are not detected as disabled
            var regularComment = "// This is a regular comment";
            var isInlineDisabledMethodRegular = typeof(NoCommentsAnalyzer)
                .GetMethod("IsInlineDisabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            var regularResult = (bool)isInlineDisabledMethodRegular.Invoke(null, new object[] { regularComment });
            Assert.IsFalse(regularResult, "Regular comment should not be detected as disabled");
        }
    }
}
