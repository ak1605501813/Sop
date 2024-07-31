using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MstConsulBuilder;
using MstCore.Filter;
using MstCore.JsonConverter;
using MstDBComman.Config;
using Swashbuckle.AspNetCore.SwaggerUI;
using MstAuth;
using MstSwagger;
using MstCore.Service;
using MstCaches;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using System.Collections.Generic;
using MstSopService.Extensions.AutoMapper;
using MstSopService.Caches;
using MstCore;
using MstSopService.IService;
using MstSopService.Service;
using MstSopService.Tools;

namespace MstSopService
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        private readonly string versionApiName = "V1";
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            MstCaches.MstCache.Configuration = configuration;
            MstCoreV3.Pub.MstPub.Configuration = configuration;
            SysConfig.InitConfig(configuration);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAutoMapperSetup();

            services.AddControllers(options =>
            {
                options.Filters.Add(typeof(GlobalExceptionsFilter));
                options.Filters.Add<ApiResultFilterAttribute>();
            }).AddNewtonsoftJson(ooptions =>
            {
                //日期类型默认格式化处理
                ooptions.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";

                //空值处理
                //ooptions.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;

                //忽略循环引用
                ooptions.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

                //高级用法九中的Bool类型转换 设置
                //setting.Converters.Add(new BoolConvert("是,否"));

                //ooptions.SerializerSettings.ContractResolver = new CustomContractResolver();
            });
            services.AddScoped<IGetPermissionUtil, GetPermissionUtil>();
            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.AddSingleton<MstCacheService>();
            services.AddSingleton<MstCoreV3.Pub.MstCache>();
            services.AddScoped<MstCaches.IMstCache, MstCaches.MstCache>();
            services.AddScoped<IGetDepartmentOrPersonUtil, GetDepartmentOrPersonUtil>();
            services.AddScoped<IShippingFrameDlagramService, ShippingFrameDlagramService>();
            services.AddScoped<IOperationSOPService, OperationSOPService>();
            services.AddScoped<ICodeNameConversionService, CodeNameConversionService>();
            services.AddScoped<IFileManagementService, FileManagementService>();
            services.AddScoped<IDictService, DictService>();
            services.AddScoped<HttpTool>();
            
            //健康检查
            services.AddHealthChecks();
            //添加Consul
            services.AddConsul();

            services.AddCors(options =>
            {
                options.AddPolicy(
                    "MyAllowSpecificOrigins",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());
            });

            services.AddSwagger(() =>
            {
                var config = new SwaggerOptions();
                config.VersionApiName = "V1";
                config.VersionApiTitle = "V1 doc";
                config.VersionXml = "MstSopService.xml";
                return config;
            });

            //验证TOKEN
            services.AddTokenJwtAuthorize(Configuration);

            services.AddSingleton<MstCoreV3.Pub.MstCache>();

            AutofacContainer.Build(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IOptions<ConsulServiceOptions> serviceOptions)
        {
            IList<CultureInfo> supportedCultures = new List<CultureInfo>
            {
                new CultureInfo("zh-CN"),
                new CultureInfo("en-US")
            };

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                //这里指定默认语言包
                DefaultRequestCulture = new RequestCulture("zh-CN"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            if (Convert.ToBoolean(SysConfig.Configuration["IsMicoService"]))
            {
                /*ServiceLocator.ApplicationBuilder = app;
                // 配置健康检测地址，.NET Core 内置的健康检测地址中间件
                app.UseHealthChecks(serviceOptions.Value.HealthCheck);
                app.UseConsul();*/
            }

            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseStaticFiles();

            app.UseCors("MyAllowSpecificOrigins");

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSwagger();
            app.UseSwaggerUI(v =>
            {
                v.SwaggerEndpoint($"/swagger/{versionApiName}/swagger.json", $"{versionApiName}");
                v.DocExpansion(DocExpansion.None);
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers()
              .RequireAuthorization("permission");
            });
        }
    }
}
