// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class PagedCharBuffer : IDisposable
    {
        public const int PageSize = 1024;

        public PagedCharBuffer(ICharBufferSource bufferSource)
        {
            BufferSource = bufferSource;
        }

        public ICharBufferSource BufferSource { get; }

        public IList<char[]> Pages { get; } = new List<char[]>();

        public char[] CurrentPage { get; private set; }

        // The next 'free' character
        public int CharIndex { get; set; }

        public void Append(char value)
        {
            var page = GetCurrentPage();
            page[CharIndex++] = value;
        }

        public void Append(string value)
        {
            if (value == null)
            {
                return;
            }

            var index = 0;
            var count = value.Length;

            while (count > 0)
            {
                var page = GetCurrentPage();
                var copyLength = Math.Min(count, page.Length - CharIndex);
                Debug.Assert(copyLength > 0);

                value.CopyTo(
                    index,
                    page,
                    CharIndex,
                    copyLength);

                CharIndex += copyLength;
                index += copyLength;

                count -= copyLength;
            }
        }

        public void Append(char[] buffer, int index, int count)
        {
            while (count > 0)
            {
                var page = GetCurrentPage();
                var copyLength = Math.Min(count, page.Length - CharIndex);
                Debug.Assert(copyLength > 0);

                Array.Copy(
                    buffer,
                    index,
                    page,
                    CharIndex,
                    copyLength);

                CharIndex += copyLength;
                index += copyLength;

                count -= copyLength;
            }
        }

        private char[] GetCurrentPage()
        {
            if (CurrentPage == null || 
                CharIndex == CurrentPage.Length)
            {
                CurrentPage = NewPage();
                CharIndex = 0;
            }

            return CurrentPage;
        }

        private char[] NewPage()
        {
            char[] page = null;
            try
            {
                page = BufferSource.Rent(PageSize);
                Pages.Add(page);
            }
            catch when (page != null)
            {
                BufferSource.Return(page);
                throw;
            }

            return page;
        }

        public void Dispose()
        {
            for (var i = 0; i < Pages.Count; i++)
            {
                BufferSource.Return(Pages[i]);
            }

            Pages.Clear();
        }
    }
}
