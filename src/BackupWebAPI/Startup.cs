﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using BackupDatabase;
using BackupDatabase.Cassandra;
using BackupWeb.Services;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;

namespace BackupWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Tokens:Key"]));

            var tokenValidationParameters = new TokenValidationParameters
            {
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                /*
                ValidateIssuer = true,
                ValidIssuer = Configuration.GetSection("TokenProviderOptions:Issuer").Value,
                ValidateAudience = true,
                ValidAudience = Configuration.GetSection("TokenProviderOptions:Audience").Value,
                */
                ValidIssuer = Configuration["Tokens:Issuer"],
                ValidAudience = Configuration["Tokens:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                /*
                options.Audience = Configuration.GetSection("TokenProviderOptions:Audience").Value;
                options.ClaimsIssuer = Configuration.GetSection("TokenProviderOptions:Issuer").Value;
                */
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = tokenValidationParameters;
                options.SaveToken = true;
            });

            var cassandraMetaDB = new CassandraMetaDB("127.0.0.1")
            {
                PasswordsKey = Encoding.UTF8.GetBytes(Configuration["Encryption:PasswordsKey"])
            };
            var cassandraUsersDB = new CassandraUsersDB("127.0.0.1");

            services.AddMvc();
            services.AddResponseCompression();

            services.AddSingleton<IMetaDBAccess>(cassandraMetaDB);
            services.AddSingleton<IUsersDBAccess>(cassandraUsersDB);
            services.AddSingleton<AgentClient>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            /*
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/api/Exception");
            }
            */
			app.UseResponseCompression();
            app.UseExceptionHandler("/api/Exception");

            /*
            app.Use(async (context, next) =>
            {
                var authenticateInfo = await context.AuthenticateAsync("Bearer");

                // Do work that doesn't write to the Response.
                await next.Invoke();
                // Do logging or other work that doesn't write to the Response.
            });
            */
			app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "catch-all",
                    template: "{*url}",
                    defaults: new { controller = "Home", action = "RedirectIndex" }
                );
            });
        }
    }
}
