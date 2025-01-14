using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Json
{
    internal sealed class TextReaderWrapper
    {
        private readonly TextReader m_reader;
        private readonly char[] m_chars = new char[1024];
        private int m_charsCount = 0;
        private int m_firstCharIndex = 0;
        private bool m_endOfReaderReached = false;

        internal TextReaderWrapper(TextReader reader)
        {
            this.m_reader = reader;
        }

        internal async Task<int> PeekAsync(CancellationToken ct)
        {
            await this.AssureChars(ct);

            if (this.m_endOfReaderReached)
            {
                return -1;
            }

            return (this.m_chars[this.m_firstCharIndex]);
        }

        internal async Task<int> ReadAsync(CancellationToken ct)
        {
            await this.AssureChars(ct);

            if (this.m_endOfReaderReached)
            {
                return -1;
            }

            int result = this.m_chars[this.m_firstCharIndex];

            ++this.m_firstCharIndex;

            return result;
        }

        private async Task AssureChars(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }

            if (this.m_endOfReaderReached)
            {
                return;
            }

            if (this.m_firstCharIndex >= this.m_charsCount)
            {
                this.m_charsCount = await this.m_reader.ReadAsync(this.m_chars, 0, this.m_chars.Length);
                this.m_firstCharIndex = 0;
                if (this.m_charsCount == 0)
                {
                    this.m_endOfReaderReached = true;
                }
            }
        }
    }
}
