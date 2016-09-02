// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class PagedBufferedTextWriter : TextWriter
    {
        private readonly TextWriter _inner;
        private readonly PagedCharBuffer _charBuffer;

        public PagedBufferedTextWriter(ArrayPool<char> pool, TextWriter inner)
        {
            _charBuffer = new PagedCharBuffer(new ArrayPoolBufferSource(pool));
            _inner = inner;
        }

        public override Encoding Encoding => _inner.Encoding;

        public override void Flush()
        {
            // Don't do anything. We'll call FlushAsync.
        }

        public override async Task FlushAsync()
        {
            var pages = _charBuffer.Pages;
            if (_charBuffer.Pages.Count == 0)
            {
                return;
            }

            for (var i = 0; i < pages.Count; i++)
            {
                var page = pages[i];

                var count = i == pages.Count - 1 ? _charBuffer.CharIndex : page.Length;
                if (count > 0)
                {
                    await _inner.WriteAsync(page, 0, count);
                }
            }

            // Return all but one of the pages. This way if someone writes a large chunk of
            // content, we can return those buffers and avoid holding them for the whole
            // page's lifetime.
            for (var i = pages.Count - 1; i > 0; i--)
            {
                var page = pages[i];

                try
                {
                    pages.RemoveAt(i);
                }
                finally
                {
                    _charBuffer.BufferSource.Return(page);
                }
            }

            _charBuffer.CharIndex = 0;
        }

        public override void Write(char value)
        {
            _charBuffer.Append(value);
        }

        public override void Write(char[] buffer)
        {
            if (buffer == null)
            {
                return;
            }

            _charBuffer.Append(buffer, 0, buffer.Length);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            _charBuffer.Append(buffer, index, count);
        }

        public override void Write(string value)
        {
            if (value == null)
            {
                return;
            }

            _charBuffer.Append(value);
        }

        public override Task WriteAsync(char value)
        {
            return _inner.WriteAsync(value);
        }

        public override Task WriteAsync(char[] buffer, int index, int count)
        {
            return _inner.WriteAsync(buffer, index, count);
        }

        public override Task WriteAsync(string value)
        {
            return _inner.WriteAsync(value);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _charBuffer.Dispose();
        }
    }
}
