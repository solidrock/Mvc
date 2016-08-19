using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.DataAnnotations.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations.Test.Internal
{
    public class MvcDataAnnotationsMvcOptionsSetupTests
    {
        [Fact]
        public void MvcDataAnnotationsMvcOptionsSetup_ServiceConstructorWithoutIStringLocalizer()
        {
            // Arrange
            var services = new ServiceCollection();

            services.AddSingleton<IHostingEnvironment>(GetHostingEnvironment());
            services.AddSingleton<IValidationAttributeAdapterProvider, ValidationAttributeAdapterProvider>();
            services.AddSingleton<IOptions<MvcDataAnnotationsLocalizationOptions>>(
                new TestOptionsManager<MvcDataAnnotationsLocalizationOptions>());
            services.AddSingleton<IConfigureOptions<MvcOptions>, MvcDataAnnotationsMvcOptionsSetup>();

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var optionsSetup = serviceProvider.GetRequiredService<IConfigureOptions<MvcOptions>>();

            // Assert
            Assert.NotNull(optionsSetup);
        }

        private IHostingEnvironment GetHostingEnvironment()
        {
            var environment = new Mock<IHostingEnvironment>();
            environment
                .Setup(e => e.ApplicationName)
                .Returns(typeof(MvcDataAnnotationsMvcOptionsSetupTests).GetTypeInfo().Assembly.GetName().Name);

            return environment.Object;
        }
    }
}
