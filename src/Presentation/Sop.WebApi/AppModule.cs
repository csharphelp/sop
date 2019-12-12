﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
using Sop.Data;
using Sop.Data.Caching;
using Sop.Data.NhRepositories;
using StackExchange.Redis;
using Module = Autofac.Module;

namespace Sop.WebApi
{

    public class AppModule : Module
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        protected override void Load(ContainerBuilder builder)
        {
            // The generic ILogger<TCategoryName> service was added to the ServiceCollection by ASP.NET Core.
            // It was then registered with Autofac using the Populate method. All of this starts
            // with the `UseServiceProviderFactory(new AutofacServiceProviderFactory())` that happens in Program and registers Autofac
            // as the service provider.
//            builder.Register(c => new ValuesService(c.Resolve<ILogger<ValuesService>>()))
//                .As<IValuesService>()
//                .InstancePerLifetimeScope();

            //获取当前相关的程序集
            var files = Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory, "Sop.*.dll");
            files = files.Union(Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory, "Sop.Common.*.dll"));
            var assemblies = files.Select(n => Assembly.Load(AssemblyName.GetAssemblyName(n))).ToArray();

 
            
            builder.RegisterAssemblyTypes(assemblies)
                   .Where(t => t.Name.EndsWith("Service") && !t.Name.Contains("CacheService"))
                   .AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope()
                   .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies|PropertyWiringOptions.PreserveSetValues);
             
            //注册Redis服务
            string redis = "127.0.0.1:6379,defaultDatabase=1,ssl=false,writeBuffer=10240";
            var option = ConfigurationOptions.Parse(redis);
            builder.Register(c => ConnectionMultiplexer.Connect(option)).SingleInstance().PropertiesAutowired();

            builder.Register(c => new RedisCacheManager(option)).As<ICacheManager>().SingleInstance()
                   .PropertiesAutowired().InstancePerRequest();

            //注册NHibernate的SessionManager
            builder.Register(@delegate: c => new AppSessionFactory(assemblies)).SingleInstance() .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies|PropertyWiringOptions.PreserveSetValues);
  

            //注册Repository
            builder.RegisterGeneric(typeof(Repository<>)).As(typeof(IRepository<>))
                   .SingleInstance().PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies|PropertyWiringOptions.PreserveSetValues);
 


        }
        


        
    }
}