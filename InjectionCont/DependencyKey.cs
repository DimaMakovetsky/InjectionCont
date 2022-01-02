using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InjectionCont
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class DependencyKey : Attribute
    {
        public ImplementationNumber ImplNumber { get; }

        public DependencyKey(ImplementationNumber number)
        {
            this.ImplNumber = number;
        }
    }
}
