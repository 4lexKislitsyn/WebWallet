using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AutoMapper;
using CadastralExchange.PackageProcessing.Web.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;
using Swashbuckle.AspNetCore.SwaggerGen;
using WebWallet.API.ExternalAPI;
using WebWallet.API.ExternalAPI.Interfaces;
using WebWallet.DB;

namespace WebWallet.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        /// <summary>
        /// Gets path to XML documentation for current assembly.
        /// </summary>
        private static string XmlCommentsFilePath
        {
            get
            {
                var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                var fileName = typeof(Startup).GetTypeInfo().Assembly.GetName().Name + ".xml";
                return System.IO.Path.Combine(basePath, fileName);
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddApiVersioning(options =>
            {
                options.ApiVersionReader = new QueryStringApiVersionReader("api-version");
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
            });

            services
                .AddTransient<IConfigureOptions<SwaggerGenOptions>, SwaggerConfigureOptions>()
                .AddVersionedApiExplorer(options =>
                {
                    options.GroupNameFormat = "'v'VVV";
                    options.SubstituteApiVersionInUrl = true;
                })
                .AddSwaggerGen(options =>
                {
                    options.OperationFilter<SwaggerDefaultValues>();

                    var docsPath = XmlCommentsFilePath;
                    if (System.IO.File.Exists(docsPath))
                    {
                        options.IncludeXmlComments(docsPath, includeControllerXmlComments: true);
                    }
                });

            services.AddMvcCore().AddNewtonsoftJson();

            services.AddSingleton<IWebWalletRepository, InMemoryRepository>();
            services.AddTransient<ICurrencyRateService, ECBCurrencyRateService>();

            services.AddAutoMapper(typeof(AutomapperProfiles.EntityToModelProfile), typeof(AutomapperProfiles.ModelToEntityProfile));

            services.Configure<ECBCurrencyConfiguration>(Configuration.GetSection(nameof(ECBCurrencyRateService)));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            var apiDesctiptionProvider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();
            app.UseSwagger().UseSwaggerUI(options =>
            {
                // build a swagger endpoint for each discovered API version
                foreach (var description in apiDesctiptionProvider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                }
            });
        }
    }
}
