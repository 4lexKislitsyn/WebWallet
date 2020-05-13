using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CadastralExchange.PackageProcessing.Web.Swagger
{

    /// <summary>
    /// Configures the Swagger generation options.
    /// </summary>
    /// <remarks>This allows API versioning to define a Swagger document per API version after the
    /// <see cref="IApiVersionDescriptionProvider"/> service has been resolved from the service container.</remarks>
    public class SwaggerConfigureOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider _provider;
        private static readonly OpenApiContact _contact = new OpenApiContact() { Name = "Kislitsyn Alexander", Url = new System.Uri("https://kislitsyn.work") };

        /// <summary>
        /// Create an instance of the <see cref="SwaggerConfigureOptions"/>.
        /// </summary>
        /// <param name="provider">The <see cref="IApiVersionDescriptionProvider"/> used to generate Swagger documents.</param>
        public SwaggerConfigureOptions(IApiVersionDescriptionProvider provider) => _provider = provider;

        /// <inheritdoc />
        public void Configure(SwaggerGenOptions options)
        {
            foreach (var description in _provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
            }
        }

        static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
        {
            var info = new OpenApiInfo()
            {
                Title = nameof(WebWallet.API),
                Version = description.ApiVersion.ToString(),
                Description = string.Empty,
                Contact = _contact
            };

            if (description.IsDeprecated)
            {
                info.Description += " This API version has been deprecated.";
            }

            return info;
        }
    }
}
