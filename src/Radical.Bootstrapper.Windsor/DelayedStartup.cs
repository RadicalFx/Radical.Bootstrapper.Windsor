using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Windsor;

namespace Radical.Bootstrapper
{
	public sealed class WindsorDelayedStartup: IDelayedStartup<IWindsorContainer>
	{
		public IWindsorContainer Container { get; internal set; }
		public Func<IWindsorContainer> Startup { get; internal set; }
	}
}
