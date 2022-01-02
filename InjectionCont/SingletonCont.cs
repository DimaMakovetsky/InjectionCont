using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InjectionCont
{
    class SingletonCont
    {
        public readonly ImplementationNumber ImplNumber;

        public readonly object Instance;

        public SingletonCont(object instance, ImplementationNumber number)
        {
            this.ImplNumber = number;
            this.Instance = instance;
        }
    }
}
