using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using GloboTicket.Web.Models;
using GloboTicket.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace GloboTicket.Web
{
    public class Startup
    {
        private readonly IHostEnvironment environment;
        private readonly IConfiguration config;

        public Startup(IConfiguration configuration, IHostEnvironment environment)
        {
            config = configuration;
            this.environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var builder = services.AddControllersWithViews();

            // INFO: Fix data protection docker
            //services.AddDataProtection().ProtectKeysWithAzureKeyVault(new Uri(config["KeyVaultKeyId"]), GetTokenCredential());
            //services.AddDataProtection().PersistKeysToAzureBlobStorage(new Uri("https://mystorageacctt.blob.core.windows.net/myblobcontainer?sp=racwdl&st=2021-04-08T14:23:14Z&se=2021-04-08T22:23:14Z&spr=https&sv=2020-02-10&sr=c&sig=Zb8LHEvo5CqrA0broEXFGjAA8NNlyHBZsAHlmmnXIaE%3D"));
            string connectionString = "DefaultEndpointsProtocol=https;EndpointSuffix=core.windows.net;AccountName=mystorageacctt;AccountKey=NF7TW1TCV8aOWa1w6H3ks4WX6/q8rK9dWNJWwOYRUXz9NlEverp2o2Z/aUWqkg51oHhMyDtLB1a9CJCzBcFkzg==";
            string containerName = "myblobcontainer";
            BlobContainerClient container = new BlobContainerClient(connectionString, containerName);

            // optional - provision the container automatically
            container.CreateIfNotExistsAsync();

            services.AddDataProtection()
                .PersistKeysToAzureBlobStorage(connectionString, containerName, "mystorageacctt");

            // INFO: Fix redirection
            services.AddHttpsRedirection(options =>
            {
                options.HttpsPort = 443;
            });

            if (environment.IsDevelopment())
                builder.AddRazorRuntimeCompilation();

            services.AddHttpClient<IEventCatalogService, EventCatalogService>(c =>
                c.BaseAddress = new Uri(config["ApiConfigs:EventCatalog:Uri"]));
            services.AddHttpClient<IShoppingBasketService, ShoppingBasketService>(c =>
                c.BaseAddress = new Uri(config["ApiConfigs:ShoppingBasket:Uri"]));
            services.AddHttpClient<IOrderService, OrderService>(c =>
                c.BaseAddress = new Uri(config["ApiConfigs:Order:Uri"]));
            services.AddHttpClient<IDiscountService, DiscountService>(c =>
                c.BaseAddress = new Uri(config["ApiConfigs:Discount:Uri"]));

            services.AddSingleton<Settings>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=EventCatalog}/{action=Index}/{id?}");
            });
        }

        private TokenCredential GetTokenCredential()
        {
            var credentialOptions = new DefaultAzureCredentialOptions();
            //if (options.SharedTokenCacheTenantId != null)
            //{
            //    credentialOptions.SharedTokenCacheTenantId = options.SharedTokenCacheTenantId;
            //}

            return new DefaultAzureCredential(credentialOptions);
        }
    }
}