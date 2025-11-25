using System;


namespace Jsonata.Net.Native.Eval
{
    //created once for each EvalProcessor.EvaluateJson call
    public sealed class EvaluationSupplement
    {
        private readonly Lazy<Random> m_random = new Lazy<Random>();
        private readonly DateTimeOffset m_now = DateTimeOffset.UtcNow;
        private int m_depth = 0;

        internal Random Random => this.m_random.Value;
        internal DateTimeOffset Now => this.m_now;

        internal int IncDepth()
        {
            ++this.m_depth;
            return this.m_depth;
        }

        internal void DecDepth()
        {
            --this.m_depth;   
        }
    }
}
