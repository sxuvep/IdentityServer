// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using IdentityServer.Models;
using Microsoft.AspNetCore.Identity;

namespace IdentityServer
{
    public class Startup
    {
        public IWebHostEnvironment Environment { get; }
        public IConfiguration Configuration { get; }

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Environment = environment;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // uncomment, if you want to add an MVC-based UI
            services.AddControllersWithViews();

            var connectionString = Configuration.GetConnectionString("defaultConnection");

            var migrationAssembly = typeof(Startup).Assembly.GetName().Name;

            //register identity db context to use sqlserver
            services.AddDbContext<IdentityDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationAssembly));
            });

            //register identity service to use idenitydb context
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
             {
                 options.SignIn.RequireConfirmedEmail = true;
             })
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();


            var builder = services.AddIdentityServer(options =>
            {

                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                options.UserInteraction.LoginUrl = "/Account/Login";
                options.UserInteraction.LogoutUrl = "/Account/Logout";

                options.Authentication = new IdentityServer4.Configuration.AuthenticationOptions
                {
                    CookieLifetime = TimeSpan.FromHours(10),
                    CookieSlidingExpiration = true
                };
            })
                //configures clients, resource and cors from db
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = builder =>
                    builder.UseSqlServer(connectionString, options => options.MigrationsAssembly(migrationAssembly));
                })
                //configures tokens, grants and consents from db
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = builder =>
                    builder.UseSqlServer(connectionString, options => options.MigrationsAssembly(migrationAssembly));
                })
                //configures users from identity store
                .AddAspNetIdentity<ApplicationUser>();

            // not recommended for production - you need to store your key material somewhere secure
            builder.AddDeveloperSigningCredential();
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            
            // uncomment if you want to add MVC
            app.UseStaticFiles();
            app.UseRouting();

            app.UseIdentityServer();

            // uncomment, if you want to add MVC
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}
