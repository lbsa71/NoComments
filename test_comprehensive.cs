using System;

namespace Test
{
    public class TestContiguousBlocks
    {
        public void TestMethod()
        {
            // NOTE: Decided against hashing this, because when we present this to a third party server for validation
            // they need to get a copy of the public key anyway.
            // This entire block should be allowed since the first line has NOTE:
            var x = 42;
            
            // This should be flagged since no marker and not contiguous with above
            var y = 43;
            
            // HUMAN: This starts a new block
            // and this continuation should also be allowed
            // even though it doesn't have a marker itself
            var z = 44;
            
            /* NOTE: Multi-line comment that starts with marker
             * continuation of the multi-line comment
             * should all be treated as one comment, not a block */
            var a = 45;
            
            // TODO; this should have normalization offered
            var b = 46;
            
            // FIXME, this should have normalization offered
            var c = 47;
        }
    }
}