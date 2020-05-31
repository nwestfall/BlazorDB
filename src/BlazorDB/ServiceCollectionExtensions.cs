using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BlazorDB
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBlazorDB(this IServiceCollection services, Action<DbStore> options)
        {
            var dbStore = new DbStore();
            options(dbStore);
            
            services.AddTransient<DbStore>((_) => dbStore);
            services.TryAddSingleton<IBlazorDbFactory, BlazorDbFactory>();
            
            return services;
        }
    }
}