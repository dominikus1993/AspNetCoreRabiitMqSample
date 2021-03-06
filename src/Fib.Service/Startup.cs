﻿using EasyNetQ;
using Fib.Common.Messages;
using Fib.Service.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Swagger;

namespace Fib.Service
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
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new Info {Title = "Fib Api", Version = "v1"}); });
            services.AddDistributedMemoryCache();
            services.RegisterEasyNetQ("host=localhost;username=guest;password=guest");
            services.AddSingleton<IFibCalculator, FibCalculator>();
            var sp = services.BuildServiceProvider();
            var service = sp.GetService<IBus>();
            service.Receive<GetFib>("Test", msg =>
            {
                var logger = sp.GetService<ILogger<Startup>>();
                logger.LogInformation($"Received {msg}");
                var res = sp.GetService<IFibCalculator>().Calculate(msg.Number);
                service.Publish(new FibCalculated(msg.Number, res));
            });
//            services.AddTransient<ICommandBus, RabbitCommandBus>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
            app.UseSwagger().UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint(
                    $"/swagger/v1/swagger.json",
                    "FibApi");
            });
        }
    }
}