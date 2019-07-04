using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AnyPackageManagerCache.Services;
using AnyPackageManagerCache.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Memory;

namespace AnyPackageManagerCache
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

            services.AddSingleton<Pypi>();
            services.AddSingleton<NpmJs>();
            services.AddSingleton<FeaturesService>();
            services.AddSingleton<WorkerService>();
            services.AddSingleton(typeof(LocalPackagesMemoryIndexes<>));
            services.AddScoped(typeof(LiteDBDatabaseService<>));
            //services.AddHostedService<PypiCacheCleanupHostedService>();
            services.AddScoped(typeof(MainService));
            services.AddScoped(typeof(MainService<>));
            services.AddSingleton<IMemoryCache, MemoryCache>();

            // background services:
            // - update services:
            services.AddSingleton(typeof(PackageIndexUpdateService<>));
            services.AddSingleton<NpmJsSyncService>();

            services.AddHostedService<BackgroundServicesLauncherHostedService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
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

            //app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
