﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using common;
using frontend.Lib;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using services;
using StackExchange.Redis;
using StructureMap;

namespace frontend
{
    public class Startup
    {
        private static ConnectionMultiplexer RedisConnection { get; set; }
        
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            RedisConnection = ConnectionMultiplexer.Connect(Configuration.GetConnectionString("redis"));
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddHangfire(options =>
            {
                options.UseRedisStorage(RedisConnection);
            });

            var container = new Container();
            container.AddHangfireFrameworkServices();
            
            container.Configure(_ =>
            {
                _.Populate(services);
            });

            return container.GetInstance<IServiceProvider>();
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
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] {new DashboardAuthorizationFilter()}
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
