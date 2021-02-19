using HotchocolateTemplate.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotchocolateTemplate
{
    public class Startup
    {
        public IConfiguration Configuration { get; private set; }
        public IHostEnvironment HostingEnvironment { get; private set; }

        public Startup(IConfiguration configuration, IHostEnvironment hostEnvironment)
        {
            this.Configuration = configuration;
            this.HostingEnvironment = hostEnvironment;
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCustomJWTAuth(this.Configuration);
            services.AddAuthorization(this.Configuration);

            services.AddGraphQLServer()
                .AddAuthorization()
                .AddSocketSessionInterceptor<AuthSocketInterceptor>()
                .AddQueryType(d => d.Name("Query"))
                    .AddTypeExtension<TestQuery>()
                .AddMutationType(d => d.Name("Mutation"))
                    .AddTypeExtension<AuthMutations>();
                // .AddSubscriptionType(d => d.Name("Subscription"));

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseWebSockets();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGraphQL();
            });
        }
    }
}
