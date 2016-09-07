// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public class CookieTempDataProvider : ITempDataProvider
    {
        public static readonly string CookieName = ".AspNetCore.Mvc.ViewFeatures.CookieTempDataProvider";

        //TODO: DOES THE LENGTH OF THIS NAME MATTER?
        private static readonly string Purpose = "Microsoft.AspNetCore.Mvc.ViewFeatures.CookieTempDataProviderToken.v1";
        private const byte TokenVersion = 0x01;
        private readonly IDataProtector _cryptoSystem;
        private readonly IDataProtector _dataProtector;
        private TempDataProviderStore _tempDataProviderStore;

        public CookieTempDataProvider(IDataProtectionProvider dataProtectionProvider)
        {
            _cryptoSystem = dataProtectionProvider.CreateProtector(Purpose);
            _dataProtector = _cryptoSystem.CreateProtector(Purpose);
            _tempDataProviderStore = new TempDataProviderStore();
        }

        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            string value;
            if (context.Request.Cookies.TryGetValue(CookieName, out value))
            {
                var base64EncodedValue = Convert.FromBase64String(value);
                var unprotectedData = _dataProtector.Unprotect(base64EncodedValue);
                return _tempDataProviderStore.DeserializeTempData(unprotectedData);
            }

            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var hasValues = (values != null && values.Count > 0);
            if (hasValues)
            {
                var bytes = _tempDataProviderStore.SerializeTempData(values);
                bytes = _dataProtector.Protect(bytes);

                context.Response.Cookies.Append(
                    CookieName,
                    Convert.ToBase64String(bytes),
                    new CookieOptions()
                    {
                        //Expires TODO: THE TIME REQUIRED
                        Path = context.Request.PathBase,
                        HttpOnly = true,
                        Secure = true
                    });
            }
            else
            {
                context.Response.Cookies.Delete(CookieName);
            }
        }
    }
}
