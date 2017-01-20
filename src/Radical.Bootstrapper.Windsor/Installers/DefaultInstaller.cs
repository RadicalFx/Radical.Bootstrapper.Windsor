using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.MicroKernel.Registration;
using System.ComponentModel.Composition;
using Castle.Windsor;
using Castle.MicroKernel.SubSystems.Configuration;
using Topics.Radical.ComponentModel.Messaging;
using Topics.Radical;
using Topics.Radical.Reflection;
using Castle.Core;
using Topics.Radical.ComponentModel;
using Topics.Radical.Threading;
using Topics.Radical.Messaging;
using Radical.Bootstrapper;
using Topics.Radical.ComponentModel.Validation;

namespace Radical.Bootstrapper.Installers
{
    /// <summary>
    /// Default boot installer.
    /// </summary>
    [Export( typeof( IWindsorInstaller ) )]
    public sealed class DefaultInstaller : IWindsorInstaller
    {
        /// <summary>
        /// Performs the installation in the <see cref="T:Castle.Windsor.IWindsorContainer"/>.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="store">The configuration store.</param>
        public void Install( IWindsorContainer container, IConfigurationStore store )
        {
            var boot = container.Resolve<IBootstrapper>();
            var directory = boot.ProbeDirectory;
            var filter = boot.AssemblyFilter;

            if ( !container.Kernel.HasComponent( typeof( IWindsorContainer ) ) )
            {
                container.Register
                (
                    Component.For<IWindsorContainer>()
                        .Instance( container )
                );
            }

            container.Register
            (
                Types.FromAssemblyInDirectory( new AssemblyFilter( directory, filter ) )
                    .IncludeNonPublicTypes()
                    .Where( t => !boot.Conventions.IsExcluded( t ) && boot.Conventions.IsService( t ) )
                    .WithService.Select( ( type, baseTypes ) => boot.Conventions.SelectServiceContracts( type ) )
                    .LifestyleSingleton()
                    .Configure( c =>
                    {
                        if ( c.Implementation.Is<IRequireToStart>() )
                        {
                            c.Forward<IRequireToStart>();
                        }
                    } )
            );

            container.Register
            (
                Types.FromAssemblyInDirectory( new AssemblyFilter( directory, filter ) )
                    .IncludeNonPublicTypes()
                    .Where( t => !boot.Conventions.IsExcluded( t ) && boot.Conventions.IsFactory( t ) )
                    .WithService.Select( ( type, baseTypes ) => boot.Conventions.SelectFactoryContracts( type ) )
                    .LifestyleSingleton()
                    .Configure( c =>
                    {
                        if ( c.Implementation.Is<IRequireToStart>() )
                        {
                            c.Forward<IRequireToStart>();
                        }
                    } )
            );

            container.Register
            (
                Types.FromAssemblyInDirectory( new AssemblyFilter( directory, filter ) )
                    .IncludeNonPublicTypes()
                    .Where( t => !boot.Conventions.IsExcluded( t ) && boot.Conventions.IsValidator( t ) )
                    .WithService.Select( ( type, baseTypes ) => boot.Conventions.SelectValidatorContracts( type ) )
                    .LifestyleSingleton()
                    .Configure( c =>
                    {
                        if ( c.Implementation.Is<IRequireToStart>() )
                        {
                            c.Forward<IRequireToStart>();
                        }
                    } )
            );
        }
    }
}
