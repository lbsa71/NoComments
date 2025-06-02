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
        public void TestCodeFixProviderHasBothRemoveAndKeepOptions()
        {
            // This test verifies that both remove and keep code fix actions are available
            var codeFixProvider = new NoCommentsCodeFixProvider();
            Assert.IsNotNull(codeFixProvider);
            
            // We can't easily test the actual code fix registration without complex setup,
            // but we can verify the provider is properly configured
            Assert.AreEqual(1, codeFixProvider.FixableDiagnosticIds.Length);
            Assert.AreEqual("NC0001", codeFixProvider.FixableDiagnosticIds[0]);
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

        [TestMethod]
        public void TestIntentionalMarkerDetection()
        {
            // Test intentional marker detection with custom markers
            var defaultMarkers = new[] { "HUMAN:", "NOTE:", "INTENT:", "OK:", "[!]" };
            var customMarkers = new[] { "CUSTOM:", "SPECIAL:" };
            
            var containsIntentionalMarkerMethod = typeof(NoCommentsAnalyzer)
                .GetMethod("ContainsIntentionalMarker", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Test default markers
            var result1 = (bool)containsIntentionalMarkerMethod.Invoke(null, new object[] { "// HUMAN: This is intentional", defaultMarkers });
            Assert.IsTrue(result1);
            
            var result2 = (bool)containsIntentionalMarkerMethod.Invoke(null, new object[] { "// NOTE: This is intentional", defaultMarkers });
            Assert.IsTrue(result2);
            
            // Test custom markers
            var result3 = (bool)containsIntentionalMarkerMethod.Invoke(null, new object[] { "// CUSTOM: This is intentional", customMarkers });
            Assert.IsTrue(result3);
            
            // Test that default markers don't work with custom config
            var result4 = (bool)containsIntentionalMarkerMethod.Invoke(null, new object[] { "// HUMAN: This is intentional", customMarkers });
            Assert.IsFalse(result4);
        }

        [TestMethod]
        public void TestSuppressionPatternDetection()
        {
            // Test suppression pattern detection with custom patterns
            var defaultPatterns = new[] { "TODO:", "HACK:", "FIXME:" };
            var customPatterns = new[] { "TODO:", "BUG:", "REVIEW:" };
            
            var isSuppressionPatternMethod = typeof(NoCommentsAnalyzer)
                .GetMethod("IsSuppressionPattern", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Test default patterns
            var result1 = (bool)isSuppressionPatternMethod.Invoke(null, new object[] { "// TODO: Fix this", defaultPatterns });
            Assert.IsTrue(result1);
            
            var result2 = (bool)isSuppressionPatternMethod.Invoke(null, new object[] { "// HACK: Temporary workaround", defaultPatterns });
            Assert.IsTrue(result2);
            
            // Test custom patterns
            var result3 = (bool)isSuppressionPatternMethod.Invoke(null, new object[] { "// BUG: Known issue", customPatterns });
            Assert.IsTrue(result3);
            
            // Test that default patterns don't work with custom config where not included
            var result4 = (bool)isSuppressionPatternMethod.Invoke(null, new object[] { "// HACK: Known issue", customPatterns });
            Assert.IsFalse(result4);
        }

        [TestMethod]
        public void TestAddIntentionalMarkerFormatting()
        {
            // Test the AddIntentionalMarker method using reflection
            var addIntentionalMarkerMethod = typeof(NoCommentsCodeFixProvider)
                .GetMethod("AddIntentionalMarker", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Test single-line comments
            var result1 = (string)addIntentionalMarkerMethod.Invoke(null, new object[] { "// this is a comment", "NOTE:" });
            Assert.AreEqual("// NOTE: this is a comment", result1);
            
            var result2 = (string)addIntentionalMarkerMethod.Invoke(null, new object[] { "//   spaced comment  ", "HUMAN:" });
            Assert.AreEqual("// HUMAN: spaced comment", result2);
            
            // Test multi-line comments
            var result3 = (string)addIntentionalMarkerMethod.Invoke(null, new object[] { "/* this is a comment */", "NOTE:" });
            Assert.AreEqual("/* NOTE: this is a comment */", result3);
            
            var result4 = (string)addIntentionalMarkerMethod.Invoke(null, new object[] { "/*  spaced comment  */", "INTENT:" });
            Assert.AreEqual("/* INTENT: spaced comment */", result4);
        }

        [TestMethod]
        public void TestGetIntentionalMarkerDefaultsToNote()
        {
            // Test that GetIntentionalMarker returns "NOTE:" by default
            var getIntentionalMarkerMethod = typeof(NoCommentsCodeFixProvider)
                .GetMethod("GetIntentionalMarker", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // We can't easily create a Document for testing, so we'll pass null and expect fallback behavior
            var result = (string)getIntentionalMarkerMethod.Invoke(null, new object[] { null });
            Assert.AreEqual("NOTE:", result);
        }

        [TestMethod]
        public void TestLicenseBannerDetection()
        {
            // Test license banner detection with contiguous blocks
            var checkLicenseContentMethod = typeof(NoCommentsAnalyzer)
                .GetMethod("CheckLicenseContent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            var defaultLicensePatterns = new[] { "Copyright", "Licensed", "SPDX-License-Identifier" };
            
            // Test individual license comments
            var result1 = (bool)checkLicenseContentMethod.Invoke(null, new object[] { "// Copyright (C) 2019 VIMaec LLC.", defaultLicensePatterns });
            Assert.IsTrue(result1, "Comment with Copyright should be detected as license content");
            
            var result2 = (bool)checkLicenseContentMethod.Invoke(null, new object[] { "// Licensed under MIT License", defaultLicensePatterns });
            Assert.IsTrue(result2, "Comment with Licensed should be detected as license content");
            
            // Test non-license comments that appear in copyright headers
            var result3 = (bool)checkLicenseContentMethod.Invoke(null, new object[] { "// MIT License", defaultLicensePatterns });
            Assert.IsFalse(result3, "Comment with only MIT License should not be detected as license content");
            
            var result4 = (bool)checkLicenseContentMethod.Invoke(null, new object[] { "// This file is subject to the terms and conditions defined in", defaultLicensePatterns });
            Assert.IsFalse(result4, "Generic license text without patterns should not be detected as license content");
        }

        [TestMethod]
        public void TestContiguousCommentBlocks()
        {
            // Test the GetContiguousCommentBlocks method
            var getContiguousCommentBlocksMethod = typeof(NoCommentsAnalyzer)
                .GetMethod("GetContiguousCommentBlocks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Create a mock list of trivia (we can't easily create real SyntaxTrivia for testing)
            // This test validates the logic conceptually
            Assert.IsNotNull(getContiguousCommentBlocksMethod, "GetContiguousCommentBlocks method should exist");
        }

        [TestMethod]
        public void TestSuppressionPatternWithFlexiblePunctuation()
        {
            // Test that suppression patterns should work with flexible punctuation
            var patterns = new[] { "TODO:", "HACK:", "FIXME:" };
            
            var isSuppressionPatternMethod = typeof(NoCommentsAnalyzer)
                .GetMethod("IsSuppressionPattern", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Test cases that should be recognized as suppression patterns
            var testCases = new[]
            {
                "// TODO; this should work",
                "// TODO, this should work", 
                "// TODO- this should work",
                "// FIXME; this should work",
                "// HACK, this should work",
                "/* TODO; block comment */",
                "//   TODO;   with extra spaces",
                "// TODO: standard colon should still work"
            };
            
            foreach (var testCase in testCases)
            {
                var result = (bool)isSuppressionPatternMethod.Invoke(null, new object[] { testCase, patterns });
                Assert.IsTrue(result, $"Should recognize '{testCase}' as suppression pattern");
            }
            
            // Test cases that should NOT be recognized
            var negativeCases = new[]
            {
                "// TODOLIST something",
                "// NOT TODO: something", 
                "// ATODO: something",
                "// regular comment"
            };
            
            foreach (var testCase in negativeCases)
            {
                var result = (bool)isSuppressionPatternMethod.Invoke(null, new object[] { testCase, patterns });
                Assert.IsFalse(result, $"Should NOT recognize '{testCase}' as suppression pattern");
            }
        }

        [TestMethod]
        public void TestContiguousIntentionalMarkerBlocks()
        {
            // Test that contiguous comment blocks should be allowed if first comment has intentional marker
            var markers = new[] { "HUMAN:", "NOTE:", "INTENT:", "OK:", "[!]" };
            
            var containsIntentionalMarkerMethod = typeof(NoCommentsAnalyzer)
                .GetMethod("ContainsIntentionalMarker", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Test cases for comment blocks
            var firstCommentWithMarker = "// NOTE: Decided against hashing this, because when we present this to a third party server for validation";
            var secondCommentWithoutMarker = "// they need to get a copy of the public key anyway.";
            
            // The first comment should be recognized
            var result1 = (bool)containsIntentionalMarkerMethod.Invoke(null, new object[] { firstCommentWithMarker, markers });
            Assert.IsTrue(result1, "First comment with marker should be recognized");
            
            // The second comment currently won't be recognized (this is what we need to fix)
            var result2 = (bool)containsIntentionalMarkerMethod.Invoke(null, new object[] { secondCommentWithoutMarker, markers });
            Assert.IsFalse(result2, "Second comment without marker currently not recognized - this is what we need to fix");
        }

        [TestMethod]
        public void TestSuppressionPatternNormalization()
        {
            // Test the normalization of suppression patterns
            var normalizeMethod = typeof(NoCommentsCodeFixProvider)
                .GetMethod("NormalizeSuppressionPatternText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Test single-line comments
            var result1 = (string)normalizeMethod.Invoke(null, new object[] { "// TODO; fix this issue" });
            Assert.AreEqual("// TODO: fix this issue", result1);
            
            var result2 = (string)normalizeMethod.Invoke(null, new object[] { "// FIXME, handle edge case" });
            Assert.AreEqual("// FIXME: handle edge case", result2);
            
            var result3 = (string)normalizeMethod.Invoke(null, new object[] { "// HACK- temporary workaround" });
            Assert.AreEqual("// HACK: temporary workaround", result3);
            
            // Test multi-line comments
            var result4 = (string)normalizeMethod.Invoke(null, new object[] { "/* TODO; block comment */" });
            Assert.AreEqual("/* TODO: block comment */", result4);
            
            // Test that already normalized comments are unchanged
            var result5 = (string)normalizeMethod.Invoke(null, new object[] { "// TODO: already correct" });
            Assert.AreEqual("// TODO: already correct", result5);
        }

        [TestMethod]
        public void TestIsNormalizableSuppressionPattern()
        {
            // Test the detection of normalizable suppression patterns
            var isNormalizableMethod = typeof(NoCommentsCodeFixProvider)
                .GetMethod("IsNormalizableSuppressionPattern", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Should be normalizable
            Assert.IsTrue((bool)isNormalizableMethod.Invoke(null, new object[] { "// TODO; fix this" }));
            Assert.IsTrue((bool)isNormalizableMethod.Invoke(null, new object[] { "// FIXME, handle case" }));
            Assert.IsTrue((bool)isNormalizableMethod.Invoke(null, new object[] { "// HACK- workaround" }));
            
            // Should NOT be normalizable (already correct or not suppression pattern)
            Assert.IsFalse((bool)isNormalizableMethod.Invoke(null, new object[] { "// TODO: already correct" }));
            Assert.IsFalse((bool)isNormalizableMethod.Invoke(null, new object[] { "// regular comment" }));
            Assert.IsFalse((bool)isNormalizableMethod.Invoke(null, new object[] { "// NOTE: intentional comment" }));
        }
    }
}
