using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace NoCommentsAnalyzer.Test
{
    [TestClass]
    public class CopyrightHeaderTests
    {
        [TestMethod]
        public void TestCopyrightHeaderBlockIsPreserved()
        {
            // Test that all comments in a contiguous copyright header block are preserved
            var sourceCode = @"// MIT License
// Copyright (C) 2019 VIMaec LLC.
// Copyright (C) 2018 Ara 3D. Inc
// Copyright (C) The Mono.Xna Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            // This should be flagged as unauthorized
            Console.WriteLine(""Hello"");
        }
    }
}";

            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = tree.GetRoot();
            
            var analyzer = new NoCommentsAnalyzer();
            var config = new AnalyzerConfiguration();
            
            // Get all comment trivia
            var commentTrivia = root.DescendantTrivia()
                .Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia))
                .ToList();
            
            // Using reflection to test the IsFileLevelBanner method
            var isFileLevelBannerMethod = typeof(NoCommentsAnalyzer)
                .GetMethod("IsFileLevelBanner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Test each comment in the copyright header - they should ALL be considered license banners
            var headerComments = commentTrivia.Take(6).ToList(); // First 6 comments are the header
            var nonHeaderComment = commentTrivia.Last(); // Last comment is in the method
            
            foreach (var comment in headerComments)
            {
                var result = (bool)isFileLevelBannerMethod.Invoke(null, new object[] { comment, root, config.LicensePatterns });
                Assert.IsTrue(result, $"Comment '{comment}' should be considered part of license banner");
            }
            
            // The comment inside the method should NOT be considered a license banner
            var nonHeaderResult = (bool)isFileLevelBannerMethod.Invoke(null, new object[] { nonHeaderComment, root, config.LicensePatterns });
            Assert.IsFalse(nonHeaderResult, "Comment inside method should not be considered license banner");
        }
    }
}