using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Castle.Core;
using Castle.Facilities.Startable;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
using Topics.Radical.ComponentModel.Messaging;
using Topics.Radical.Diagnostics;
using Topics.Radical.Linq;

namespace Radical.Bootstrapper
{
    public class WindsorBootstrapper : IDisposable, IBootstrapper, IBootstrapper<IWindsorContainer>
    {
        static readonly TraceSource logger = new TraceSource( typeof( WindsorBootstrapper ).Namespace );

        ~WindsorBootstrapper()
        {
            this.Dispose( false );
        }

        void Dispose( Boolean disposing )
        {
            if ( disposing )
            {

                if ( this.windsorContainer != null )
                {
                    this.windsorContainer.Dispose();
                }

                if ( this.aggregateCatalog != null )
                {
                    this.aggregateCatalog.Dispose();
                }

                if ( this.mefContainer != null )
                {
                    this.mefContainer.Dispose();
                }
            }

            this.windsorContainer = null;
            this.aggregateCatalog = null;
            this.mefContainer = null;
            this.applicationObjects = null;
        }

        public void Dispose()
        {
            this.Dispose( true );
            GC.SuppressFinalize( this );
        }

        static readonly Object syncRoot = new Object();

        Boolean _isInitialized;

        IWindsorContainer windsorContainer;
        CompositionContainer mefContainer;
        AggregateCatalog aggregateCatalog;

        public String ProbeDirectory { get; private set; }
        public String AssemblyFilter { get; private set; }
        public BootstrapConventions Conventions { get; private set; }

        public WindsorBootstrapper( string directory, string filter = "*.dll" )
        {
            this.ProbeDirectory = directory;
            this.AssemblyFilter = filter;
            this.Conventions = new BootstrapConventions();

            this.aggregateCatalog = new AggregateCatalog( new DirectoryCatalog( this.ProbeDirectory, this.AssemblyFilter ) );

            if ( filter != "*.dll" && filter != "*.*" )
            {
                this.AddCatalog( new AggregateCatalog( new DirectoryCatalog( this.ProbeDirectory, "Radical.Bootstrapper.*" ) ) );
            }
        }

        [ImportMany]
        IEnumerable<IWindsorInstaller> Installers { get; set; }

        [ImportMany]
        IEnumerable<IFacility> Facilities { get; set; }

        [ImportMany]
        IEnumerable<IConfigurator> Configurators { get; set; }

        /// <summary>
        /// Gets the container.
        /// </summary>
        /// <returns>The current windsor container.</returns>
        public IWindsorContainer GetContainer()
        {
            return this.windsorContainer;
        }

        public IDelayedStartup<IWindsorContainer> DelayedBoot()
        {
            if ( !this._isInitialized )
            {
                lock ( syncRoot )
                {
                    if ( !this._isInitialized )
                    {
                        this.windsorContainer = new Castle.Windsor.WindsorContainer();
                        this.windsorContainer.AddFacility<StartableFacility>();
                        //this.windsorContainer.AddFacility<RequiredDependencyFacility>();
                        //this.windsorContainer.AddFacility<SubscribeToMessageFacility>();

                        //this.windsorContainer.Kernel.ComponentModelBuilder.AddContributor( new RequiredDependenciesContributor() );
                        this.windsorContainer.Kernel.Resolver.AddSubResolver( new ArrayResolver( this.windsorContainer.Kernel, true ) );

                        try
                        {
                            this.mefContainer = new CompositionContainer( this.aggregateCatalog );
                            this.mefContainer.ComposeParts( this );
                        }
                        catch ( ReflectionTypeLoadException tpe )
                        {
                            foreach ( var e in tpe.LoaderExceptions )
                            {
                                Debug.WriteLine( e.Message );
                            }

                            throw;
                        }

                        foreach ( var facility in this.Facilities )
                        {
                            this.windsorContainer.AddFacility( facility );
                        }

                        this.windsorContainer.Register
                        (
                            Component.For<IBootstrapper>().Instance( this )
                        );

                        this.windsorContainer.Register
                        (
                            Component.For<CompositionContainer>().Instance( this.mefContainer )
                        );

                        var wrapper = new ServiceProviderWrapper( this.windsorContainer );

                        this.windsorContainer.Register
                        (
                            Component.For<IServiceProvider>()
                                .Instance( wrapper )
                        );

                        this.windsorContainer.Install( this.Installers.ToArray() );

                        foreach ( var cfg in this.Configurators )
                        {
                            cfg.Configure( wrapper );
                        }

                        this._isInitialized = true;
                    }
                }
            }

            return new WindsorDelayedStartup()
            {
                Container = this.windsorContainer,
                Startup = () =>
                {
                    this.windsorContainer.ResolveAll<IRequireToStart>().ForEach( o => o.Start() );
                    return this.windsorContainer;
                }
            };
        }

        public IWindsorContainer Boot()
        {
            var delayer = this.DelayedBoot();
            delayer.Startup();

            return delayer.Container;
        }

        public void Shutdown()
        {
            this.applicationObjects.Clear();

            this.Dispose();
        }

        public void AddCatalog( ComposablePartCatalog catalog )
        {
            this.aggregateCatalog.Catalogs.Add( catalog );
        }

        Dictionary<String, Object> applicationObjects = new Dictionary<string, object>();

        public void AddApplicationObject( string identifier, object instance )
        {
            this.applicationObjects.Add( identifier, instance );
        }

        public T GetApplicationObject<T>( string identifier )
        {
            return ( T )this.applicationObjects[ identifier ];
        }

        public CompositionContainer GetCompositionContainer()
        {
            return this.mefContainer;
        }
    }
}
