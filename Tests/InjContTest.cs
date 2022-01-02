using NUnit.Framework;
using System;
using InjectionCont;
using Tests;
using System.Collections.Concurrent;
using System.Threading;
using System.Collections.Generic;
using LifeCycle = InjectionCont.LifeCycle;


namespace Tests
{
    [TestFixture]
    public class DependencyInjectionContainerTests
    {
        DependenciesConfig dependencies;
        DependenciesConfig dependencies1;
        ConcurrentDictionary<int, IInterface> concurrectDict = new ConcurrentDictionary<int, IInterface>();

        [SetUp]
        public void Setup()
        {
            dependencies = new DependenciesConfig();
            dependencies.Register<IInterface, Class>();
            dependencies.Register<IStrange, Strange>();

            dependencies1 = new DependenciesConfig();
            dependencies1.Register<IInterface, Class>();
            dependencies1.Register<IInterface, Class2>();
            dependencies1.Register<IStrange, Strange>(LifeCycle.InstancePerDependency, ImplementationNumber.First);
            dependencies1.Register<IStrange, Strange2>(LifeCycle.InstancePerDependency, ImplementationNumber.Second);
        }

        [Test]
        public void DependepciesAreRegisteredTest()
        {
            bool hasInterf = dependencies.DependenciesDictionary.ContainsKey(typeof(IInterface));
            bool hasStrange = dependencies.DependenciesDictionary.ContainsKey(typeof(IStrange));
            int num = dependencies.DependenciesDictionary.Keys.Count;
            int expectedNum = 2;
            Assert.IsTrue(hasInterf, "Dependency dictionary hasn't key IInterface.");
            Assert.IsTrue(hasStrange, "Dependency dictionary hasn't key IStrange.");
            Assert.AreEqual(num, expectedNum, "Dependency dictionary has another number of keys.");
        }

        [Test]
        public void MultipleDependenciesRegisteredTest()
        {
            var containers = dependencies1.DependenciesDictionary[typeof(IInterface)];
            var type1 = containers[0].ImplementationsType;
            var type2 = containers[1].ImplementationsType;
            int num = dependencies1.DependenciesDictionary.Keys.Count;
            Assert.AreEqual(containers.Count, 2, "Wrong number of dependencies of IInterface.");
            Assert.AreEqual(type1, typeof(Class), "Another type of class Class in container.");
            Assert.AreEqual(type2, typeof(Class2), "Another type of class Class2 in container.");
            Assert.AreEqual(num, 2, "Dependency dictionary has another number of keys.");
        }

        [Test]
        public void SingleDependencyProviderTest()
        {
            var provider = new DependencyProvider(dependencies);
            var result = provider.Resolve<IStrange>();
            var innerInterface = ((Strange)result).iInterface;
            Assert.AreEqual(result.GetType(), typeof(Strange), "Wrong type of resolving result.");
            Assert.IsTrue(innerInterface != null, "Error in creating an instance of dependency.");
            Assert.AreEqual(innerInterface.GetType(), typeof(Class), "Wrong type of created dependency.");
        }

        [Test]
        public void MultipleDependencyProviderTest()
        {
            var provider = new DependencyProvider(dependencies1);
            var result = provider.Resolve<IStrange>();
            var innerInterface = ((Strange2)result).iInterface;
            Assert.AreEqual(innerInterface.GetType(), typeof(Class2), "Wrong type of created instance.");
        }

        [Test]
        public void SingletonDependencyTest()
        {
            var dep1 = new DependenciesConfig();
            dep1.Register<IInterface, Class>(LifeCycle.Singleton);
            dep1.Register<IStrange, Strange>(LifeCycle.Singleton);
            var provider = new DependencyProvider(dep1);
            var obj11 = provider.Resolve<IStrange>();
            var obj12 = provider.Resolve<IStrange>();
            var b1 = obj11 == obj12;
            int count1 = provider._singletons.Count;
            Assert.AreEqual(count1, 2, "Wrong number of Singleton objects in Dictionary for Singleton");
            Assert.IsTrue(b1, "Different objects for singleton object.");

        }

        [Test]
        public void InstancePerDependencyTest()
        {
            var dep2 = new DependenciesConfig();
            dep2.Register<IInterface, Class>(LifeCycle.InstancePerDependency);
            dep2.Register<IStrange, Strange>(LifeCycle.InstancePerDependency);
            var provider2 = new DependencyProvider(dep2);
            var obj21 = provider2.Resolve<IStrange>();
            var obj22 = provider2.Resolve<IStrange>();
            var b2 = obj21 != obj22;
            int count2 = provider2._singletons.Count;
            Assert.AreEqual(count2, 0, "Wrong number of Singleton objects in Dictionary for InstancePerDependency");
            Assert.IsTrue(b2, "Two objects should differ from each other if their dependency was registered with InstancePerDependency lifecycle");
        }

        [Test]
        public void ImplementationNumberProviderTest()
        {
            var provider = new DependencyProvider(dependencies1);
            var result = provider.Resolve<IStrange>(ImplementationNumber.First);
            var result1 = provider.Resolve<IStrange>(ImplementationNumber.Second);
            Assert.AreEqual(result.GetType(), typeof(Strange), "Wrong type for First dependency.");
            Assert.AreEqual(result1.GetType(), typeof(Strange2), "Wrong type for Second dependency");
        }

        [Test]
        public void NotValidConfigurationTest()
        {
            var config = new DependenciesConfig();
            config.Register<IStrange, Strange>();
            Assert.Throws<ArgumentException>(delegate { new DependencyProvider(config); });
        }

        [Test]
        public void DeepNestingTest()
        {
            var config = new DependenciesConfig();
            config.Register<A, AImpl>();
            config.Register<B, BImpl>();
            config.Register<C, CImpl>();
            config.Register<D, DImpl>();
            config.Register<E, EImpl>();
            config.Register<G, GImpl>(LifeCycle.Singleton);

            var provider = new DependencyProvider(config);
            A a = provider.Resolve<A>();
            B b = provider.Resolve<B>();
            C c = provider.Resolve<C>();
            D d = provider.Resolve<D>();
            E e = provider.Resolve<E>();
            G g = provider.Resolve<G>();

            var bDepG = b.GetG();
            var eDepG = e.GetG();

            bool singletonsRefersToSameObject = bDepG == eDepG;

            Assert.IsTrue(singletonsRefersToSameObject);
        }

        [Test]
        public void MultithreadingSingletonTest()
        {
            var config = new DependenciesConfig();
            config.Register<IInterface, Class>(LifeCycle.Singleton);
            DependencyProvider provider = new DependencyProvider(config);
            List<Thread> runnedThreads = new List<Thread>();
            for (int i = 0; i < 1000; i++)
            {
                ThreadStart start = new ThreadStart(delegate { ThreadCreator(provider, i); });
                Thread t = new Thread(start);
                runnedThreads.Add(t);
            }
            foreach (var t in runnedThreads)
            {
                t.Start();
            }
            foreach (var t in runnedThreads)
            {
                t.Join();
            }
            var ClassExpected = provider.Resolve<IInterface>();
            foreach (var t in runnedThreads)
            {
                var ClassActual = concurrectDict[t.ManagedThreadId];
                bool eq = ClassExpected == ClassActual;
                Assert.IsTrue(eq);
            }
        }

        private void ThreadCreator(DependencyProvider provider, int index)
        {
            concurrectDict[Thread.CurrentThread.ManagedThreadId] = provider.Resolve<IInterface>();
        }
    }



    interface IInterface
    {
        void method1();

        void method2();
    }

    class Class : IInterface
    {

        public void method1()
        {
            throw new System.NotImplementedException();
        }

        public void method2()
        {
            throw new System.NotImplementedException();
        }
    }

    interface IStrange
    {
        void mth1();

        void mth2();
    }

    class Strange : IStrange
    {
        public IInterface iInterface;

        public Strange(IInterface iInterface)
        {
            this.iInterface = iInterface;
        }

        public void mth1()
        {
            throw new System.NotImplementedException();
        }

        public void mth2()
        {
            throw new System.NotImplementedException();
        }
    }

    class Strange2 : IStrange
    {
        public IInterface iInterface;

        public Strange2(IInterface iInterface)
        {
            this.iInterface = iInterface;
        }

        public void mth1()
        {
            throw new System.NotImplementedException();
        }

        public void mth2()
        {
            throw new System.NotImplementedException();
        }
    }

    class Class2 : IInterface
    {
        public void method1()
        {
            throw new System.NotImplementedException();
        }

        public void method2()
        {
            throw new System.NotImplementedException();
        }
    }
}
