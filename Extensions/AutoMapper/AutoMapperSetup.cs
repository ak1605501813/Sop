

namespace MstSopService.Extensions.AutoMapper
{
    using System;
    using global::AutoMapper;
    using Microsoft.Extensions.DependencyInjection;
    using MstDB;
    public static class AutoMapperSetup
    {
         
        /// <summary>
        /// 扩展方法
        /// </summary>
        /// <param name="services">服务容器</param>
        public static void AddAutoMapperSetup(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var mapperConfiguration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new CustomProfile());
            });
            services.AddSingleton(sp => mapperConfiguration.CreateMapper());

            services.AddScoped<IDatabase, SugarRepository>(provider =>
            {
                var tmp = new SugarRepository();
                tmp.Instance();

                return tmp;
            });
        }
    }
}
