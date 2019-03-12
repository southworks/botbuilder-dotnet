﻿// <copyright file="UnityConfig.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
using System;
using Microsoft.Bot.Builder.Integration;
using Unity;

namespace Microsoft.Bot.Builder.TestBot.WebApi
{
    /// <summary>
    /// Specifies the Unity configuration for the main container.
    /// </summary>
    public static class UnityConfig
    {
        #pragma warning disable SA1124 // Do not use regions
        #region Unity Container
        private static Lazy<IUnityContainer> container =
        #pragma warning restore SA1124 // Do not use regions
          new Lazy<IUnityContainer>(() =>
          {
              var container = new UnityContainer();
              RegisterTypes(container);
              return container;
          });

        /// <summary>
        /// Gets configured Unity Container.
        /// </summary>
        /// <value>
        /// Configured Unity Container.
        /// </value>
        public static IUnityContainer Container => container.Value;
        #endregion

        /// <summary>
        /// Registers the type mappings with the Unity container.
        /// </summary>
        /// <param name="container">The unity container to configure.</param>
        /// <remarks>
        /// There is no need to register concrete types such as controllers or
        /// API controllers (unless you want to change the defaults), as Unity
        /// allows resolving a concrete type even if it was not previously
        /// registered.
        /// </remarks>
        public static void RegisterTypes(IUnityContainer container)
        {
            // NOTE: To load from web.config uncomment the line below.
            // Make sure to add a Unity.Configuration to the using statements.
            // container.LoadConfiguration();
            // TODO: Register your type's mappings here.
            // container.RegisterType<IProductRepository, ProductRepository>();
            var options = new BotFrameworkOptions();

            options.Middleware.Add(new ShowTypingMiddleware());

            var botFrameworkAdapter = new BotFrameworkAdapter(options.CredentialProvider)
            {
                OnTurnError = options.OnTurnError,
            };

            foreach (var middleware in options.Middleware)
            {
                botFrameworkAdapter.Use(middleware);
            }

            // return botFrameworkAdapter;
            var adapter = new InteceptorAdapter(botFrameworkAdapter);

            container.RegisterInstance<IAdapterIntegration>(adapter);

            container.RegisterType<IBot, TestBot>();
        }
    }
}
