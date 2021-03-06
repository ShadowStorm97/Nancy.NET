﻿namespace Nancy.NET
{
    using Nancy;
    using Nancy.Conventions;

    public class Bootstrapper : DefaultNancyBootstrapper
    {
        // The bootstrapper enables you to reconfigure the composition of the framework,
        // by overriding the various methods and properties.
        // For more information https://github.com/NancyFx/Nancy/wiki/Bootstrapper
        protected override void ConfigureConventions(NancyConventions conventions)
        {
            base.ConfigureConventions(conventions);

            conventions.StaticContentsConventions.Add(
                StaticContentConventionBuilder.AddDirectory("Scripts", @"Scripts")
            );
        }

          protected override IRootPathProvider RootPathProvider
          {
               get { return new CustomRootPathProvider(); }
           }
    }

    public class CustomRootPathProvider : IRootPathProvider
    {
        public string GetRootPath()
        {
            return System.AppDomain.CurrentDomain.BaseDirectory;
        }
    }
}