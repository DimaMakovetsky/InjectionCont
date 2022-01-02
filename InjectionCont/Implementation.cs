using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InjectionCont
{
    public class Implementation
    {
        public Type ImplementationsType { get; }
        public LifeCycle TimeToLive { get; }
        public ImplementationNumber ImplNumber { get; }

        public Implementation(Type implementationsType, LifeCycle timeToLive, ImplementationNumber implNumber)
        {
            this.ImplNumber = implNumber;
            this.ImplementationsType = implementationsType;
            this.TimeToLive = timeToLive;
        }
    }
}
