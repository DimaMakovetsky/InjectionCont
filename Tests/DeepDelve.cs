using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    public interface A
    {

    }

    public class AImpl : A
    {
        public AImpl(B b)
        {

        }
    }

    public interface B
    {
        G GetG();
    }

    public class BImpl : B
    {

        public G g;

        public BImpl(C c, G g)
        {
            this.g = g;
        }

        public G GetG()
        {
            return this.g;
        }
    }

    public interface C
    {

    }

    public class CImpl : C
    {
        public CImpl(D d, E e)
        {

        }
    }

    public interface D
    {

    }

    public class DImpl : D
    {

    }

    public interface E
    {
        G GetG();
    }

    public class EImpl : E
    {
        public G g;

        public EImpl(G g)
        {
            this.g = g;
        }

        public G GetG()
        {
            return this.g;
        }
    }

    public interface G
    {

    }

    public class GImpl : G
    {

    }
}
