/*
 * Copyright (c) 2025 Test Company
 * Licensed under MIT License
 */

using System;

namespace NoCommentsAnalyzer.Test
{
    /// <summary>
    /// Test class to validate the new comment rules
    /// </summary>
    public class TestValidation
    {
        public void TestNewCommentMarkers()
        {
            // HUMAN: This should be allowed
            Console.WriteLine("Test 1");
            
            // NOTE: This should be allowed
            var x = 42;
            
            // INTENT: This should be allowed
            var y = x * 2;
            
            // OK: This should be allowed
            Console.WriteLine(y);
            
            // [!] Legacy marker should still work
            var z = y + 1;
            
            // TODO: This should be allowed
            // HACK: This should be allowed
            // FIXME: This should be allowed
        }
    }
}