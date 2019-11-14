// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// Comment out the next line to use CosmosDb instead of InMemory for the anchor cache.
#define INMEMORY_DEMO

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharingService.Data;

namespace SharingService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            // Register the anchor key cache.
#if INMEMORY_DEMO
            services.AddSingleton<IAnchorKeyCache>(new MemoryAnchorCache());
#else
            services.AddSingleton<IAnchorKeyCache>(new CosmosDbCache(this.Configuration.GetValue<string>("StorageConnectionString")));
#endif

            // Add an http client
            services.AddHttpClient<SpatialAnchorsTokenService>();

            // Register the Swagger services
            services.AddSwaggerDocument(doc => doc.Title = $"{nameof(SharingService)} API");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Register the Swagger generator and the Swagger UI middlewares
            app.UseOpenApi();
            app.UseSwaggerUi3();

            app.UseHttpsRedirection();

            app.UseRewriter(
                new RewriteOptions()
                    .AddRedirect("^$","swagger")
                );

            app.UseMvc();
        }
    }
}
