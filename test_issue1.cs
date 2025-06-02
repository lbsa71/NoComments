using System;

namespace Test
{
    public class TestIssue1
    {
        public void TestMethod()
        {
            // NOTE: Decided against hashing this, because when we present this to a third party server for validation
            // they need to get a copy of the public key anyway.
            var x = 42;
            
            // This should be flagged since no marker
            // this is a continuation of the above
            var y = 43;
        }
    }
}