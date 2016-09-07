// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public class CookieTempDataProviderTest
    {
        [Fact]
        public void Load_ReturnsEmptyDictionary_WhenNoCookieDataIsAvailable()
        {
            // Arrange
            var testProvider = new CookieTempDataProvider(new PassThroughDataProtectionProvider(new PassThroughDataProtector()));

            // Act
            var tempDataDictionary = testProvider.LoadTempData(new DefaultHttpContext());

            // Assert
            Assert.Empty(tempDataDictionary);
        }

        [Fact]
        public void LoadTempData_Base64DecodesAnd_UnprotectsData_FromCookie()
        {
            // Arrange
            var expectedValues = new Dictionary<string, object>();
            expectedValues.Add("int", 10);
            var tempDataProviderStore = new TempDataProviderStore();
            var serializedData = tempDataProviderStore.SerializeTempData(expectedValues);
            var base64EncodedData = Convert.ToBase64String(serializedData);
            var dataProtector = new PassThroughDataProtector();
            var tempDataProvider = new CookieTempDataProvider(new PassThroughDataProtectionProvider(dataProtector));
            var requestCookies = new RequestCookieCollection(new Dictionary<string, string>()
            {
                { CookieTempDataProvider.CookieName, base64EncodedData }
            });
            var httpContext = new Mock<HttpContext>();
            httpContext
               .Setup(hc => hc.Request.Cookies)
               .Returns(requestCookies);

            // Act
            var actualValues = tempDataProvider.LoadTempData(httpContext.Object);

            // Assert
            Assert.Equal(serializedData, dataProtector.DataToUnprotect);
            Assert.Equal(expectedValues, actualValues);
        }

        [Fact]
        public void SaveTempData_ProtectsAnd_Base64EncodesDataAnd_SetsCookie()
        {
            // Arrange
            var values = new Dictionary<string, object>();
            values.Add("int", 10);
            var tempDataProviderStore = new TempDataProviderStore();
            var serializedData = tempDataProviderStore.SerializeTempData(values);
            var base64EncodedData = Convert.ToBase64String(serializedData);
            var dataProtector = new PassThroughDataProtector();
            var tempDataProvider = new CookieTempDataProvider(new PassThroughDataProtectionProvider(dataProtector));
            var responseCookies = new MockResponseCookieCollection();
            var httpContext = new Mock<HttpContext>();
            httpContext
                .SetupGet(hc => hc.Request.PathBase)
                .Returns("/");
            httpContext
                .Setup(hc => hc.Response.Cookies)
                .Returns(responseCookies);

            // Act
            tempDataProvider.SaveTempData(httpContext.Object, values);

            // Assert
            Assert.Equal(1, responseCookies.Count);
            Assert.Equal(base64EncodedData, responseCookies.Value);
            Assert.Equal(serializedData, dataProtector.PlainTextToProtect);
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/vdir1")]
        public void SaveTempData_SetsCookie_WithAppropriateCookieOptions(string pathBase)
        {
            // Arrange
            var values = new Dictionary<string, object>();
            values.Add("int", 10);
            var tempDataProviderStore = new TempDataProviderStore();
            var serializedData = tempDataProviderStore.SerializeTempData(values);
            var base64EncodedData = Convert.ToBase64String(serializedData);
            var dataProtector = new PassThroughDataProtector();
            var tempDataProvider = new CookieTempDataProvider(new PassThroughDataProtectionProvider(dataProtector));
            var responseCookies = new MockResponseCookieCollection();
            var httpContext = new Mock<HttpContext>();
            httpContext
                .SetupGet(hc => hc.Request.PathBase)
                .Returns(pathBase);
            httpContext
                .Setup(hc => hc.Response.Cookies)
                .Returns(responseCookies);

            // Act
            tempDataProvider.SaveTempData(httpContext.Object, values);

            // Assert
            Assert.Equal(1, responseCookies.Count);
            Assert.Equal(base64EncodedData, responseCookies.Value);
            Assert.Equal(serializedData, dataProtector.PlainTextToProtect);
            Assert.Equal(pathBase, responseCookies.Options.Path);
            Assert.True(responseCookies.Options.Secure);
            Assert.True(responseCookies.Options.HttpOnly);
        }

        private class MockResponseCookieCollection : IResponseCookies
        {
            public string Key { get; set; }
            public string Value { get; set; }
            public CookieOptions Options { get; set; }
            public int Count { get; set; }

            public void Append(string key, string value, CookieOptions options)
            {
                this.Key = key;
                this.Value = value;
                this.Options = options;
                this.Count++;
            }

            public void Append(string key, string value)
            {
                throw new NotImplementedException();
            }

            public void Delete(string key, CookieOptions options)
            {
                throw new NotImplementedException();
            }

            public void Delete(string key)
            {
                throw new NotImplementedException();
            }
        }

        private class PassThroughDataProtectionProvider : IDataProtectionProvider
        {
            private readonly IDataProtector _dataProtector;

            public PassThroughDataProtectionProvider(IDataProtector dataProtector)
            {
                _dataProtector = dataProtector;
            }

            public IDataProtector CreateProtector(string purpose)
            {
                return _dataProtector;
            }
        }

        private class PassThroughDataProtector : IDataProtector
        {
            public byte[] DataToUnprotect { get; private set; }
            public byte[] PlainTextToProtect { get; private set; }
            public string Purpose { get; private set; }

            public IDataProtector CreateProtector(string purpose)
            {
                Purpose = purpose;
                return this;
            }

            public byte[] Protect(byte[] plaintext)
            {
                PlainTextToProtect = plaintext;
                return PlainTextToProtect;
            }

            public byte[] Unprotect(byte[] protectedData)
            {
                DataToUnprotect = protectedData;
                return DataToUnprotect;
            }
        }
    }
}