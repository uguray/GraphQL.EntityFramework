﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GraphQL.Types.Relay;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.EntityFramework
{
    public static class EfGraphQLConventions
    {
        public static void RegisterInContainer(Action<Type, object> registerInstance, DbContext dbContext)
        {
            Scalars.RegisterInContainer(registerInstance);
            ArgumentGraphs.RegisterInContainer(registerInstance);

            var service = new EfGraphQLService(GetNavigationProperties(dbContext));
            registerInstance(typeof(EfGraphQLService), service);
        }

        public static void RegisterInContainer(IServiceCollection services, DbContext dbContext)
        {
            RegisterInContainer((type, instance) => { services.AddSingleton(type, instance); }, dbContext);
        }

        public static void RegisterConnectionTypesInContainer(IServiceCollection services)
        {
            services.AddTransient(typeof(ConnectionType<>));
            services.AddTransient(typeof(EdgeType<>));
            services.AddSingleton<PageInfoType>();
        }

        static Dictionary<Type, List<Navigation>> GetNavigationProperties(DbContext dbContext)
        {
            var dictionary = new Dictionary<Type, List<Navigation>>();
            foreach (var entity in dbContext.Model.GetEntityTypes())
            {
                var navigations = entity.GetNavigations();
                var value = navigations
                    .Select(
                        x => new Navigation
                        {
                            PropertyName = x.Name,
                            PropertyType = GetNavigationType(x)
                        })
                    .ToList();
                dictionary.Add(entity.ClrType, value);
            }

            return dictionary;
        }

        static Type GetNavigationType(INavigation navigation)
        {
            var navigationType = navigation.ClrType;
            var collectionType = navigationType.GetInterfaces()
                .SingleOrDefault(x => x.IsGenericType &&
                                      x.GetGenericTypeDefinition() == typeof(ICollection<>));
            if (collectionType == null)
            {
                return navigationType;
            }

            return collectionType.GetGenericArguments().Single();
        }

        public static void RegisterConnectionTypesInContainer(Action<Type> register)
        {
            register(typeof(ConnectionType<>));
            register(typeof(EdgeType<>));
            register(typeof(PageInfoType));
        }
    }
    [DebuggerDisplay("PropertyName = {PropertyName}, PropertyType = {PropertyType}")]
    public class Navigation
    {
        public string PropertyName;
        public Type PropertyType;
    }
}