﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Windsor;

namespace Radical.Bootstrapper
{
    /// <summary>
    /// 
    /// </summary>
    public class ServiceProviderWrapper : IServiceProvider, IServiceProviderWrapper
    {
		readonly IWindsorContainer container;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceProviderWrapper" /> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public ServiceProviderWrapper( IWindsorContainer container )
        {
            this.container = container;
        }

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        /// <returns>
        /// A service object of type <paramref name="serviceType" />.
        /// -or-
        /// null if there is no service object of type <paramref name="serviceType" />.
        /// </returns>
        public object GetService( Type serviceType )
        {
            if ( this.container.Kernel.HasComponent( serviceType ) )
            {
                return this.container.Resolve( serviceType );
            }

            return null;
        }

		public TContainer Unwrap<TContainer>()
		{
			return (TContainer)this.container;
		}
	}
}
