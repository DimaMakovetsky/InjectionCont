using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InjectionCont
{
    public class DependencyProvider
    {
        private Mutex mutex = new Mutex();
        private readonly DependenciesConfig _configuration;
        public readonly Dictionary<Type, List<SingletonCont>> _singletons;

        public DependencyProvider(DependenciesConfig configuration)
        {
            ConfigValidator configValidator = new ConfigValidator();
            if (!configValidator.Validate(configuration))
            {
                throw new ArgumentException("Wrong configuration");
            }

            this._singletons = new Dictionary<Type, List<SingletonCont>>();
            this._configuration = configuration;
        }

        public TDependency Resolve<TDependency>(ImplementationNumber number = ImplementationNumber.Any)
            where TDependency : class
        {
            return (TDependency)Resolve(typeof(TDependency), number);
        }

        public object Resolve(Type dependencyType, ImplementationNumber number = ImplementationNumber.Any)
        {
            mutex.WaitOne();
            object result;
            if (this.IsIEnumerable(dependencyType))
            {
                result = CreateEnumerable(dependencyType.GetGenericArguments()[0]);
            }
            else
            {
                Implementation container = GetImplContainerByDependencyType(dependencyType, number);
                Type requiredType = GetGeneratedType(dependencyType, container.ImplementationsType);
                result = this.ResolveNonIEnumerable(requiredType, container.TimeToLive, dependencyType, container.ImplNumber);
            }
            mutex.ReleaseMutex();
            return result;
        }

        private object ResolveNonIEnumerable(Type implType, LifeCycle ttl, Type dependencyType,
            ImplementationNumber number)
        {
            if (ttl != LifeCycle.Singleton)
            {
                return CreateInstance(implType);
            }

            if (IsInSingletons(dependencyType, implType, number))
            {
                return this._singletons[dependencyType]
                    .Find(singletonContainer => number.HasFlag(singletonContainer.ImplNumber)).Instance;
            }

            var result = CreateInstance(implType);
            this.AddToSingletons(dependencyType, result, number);
            return result;
        }

        private Implementation GetImplContainerByDependencyType(Type dependencyType, ImplementationNumber number)
        {
            Implementation container;
            if (dependencyType.IsGenericType)
            {
                container = GetImplementationsContainerLast(dependencyType, number);
                if (container==null)
                {
                    container = GetImplementationsContainerLast(dependencyType.GetGenericTypeDefinition(), number);
                }
                //container ??= GetImplementationsContainerLast(dependencyType.GetGenericTypeDefinition(), number);
            }
            else
            {
                container = GetImplementationsContainerLast(dependencyType, number);
            }

            return container;
        }

        private bool IsIEnumerable(Type dependencyType)
        {
            return dependencyType.GetInterfaces().Any(i => i.Name == "IEnumerable");
        }

        private object CreateInstance(Type implementationType)
        {
            var constructors = implementationType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            foreach (var constructor in constructors)
            {
                var constructorParams = constructor.GetParameters();
                var generatedParams = new List<dynamic>();
                foreach (var parameterInfo in constructorParams)
                {
                    dynamic parameter;
                    if (parameterInfo.ParameterType.IsInterface)
                    {
                        var number = parameterInfo.GetCustomAttribute<DependencyKey>()?.ImplNumber ?? ImplementationNumber.Any;
                        parameter = Resolve(parameterInfo.ParameterType, number);
                    }
                    else
                    {
                        break;
                    }
                    generatedParams.Add(parameter);
                }

                return constructor.Invoke(generatedParams.ToArray());
            }

            throw new ArgumentException("Cannot create instance of class");
        }

        private Type GetGeneratedType(Type dependencyType, Type implementationType)
        {
            if (dependencyType.IsGenericType && implementationType.IsGenericTypeDefinition)
            {
                return implementationType.MakeGenericType(dependencyType.GetGenericArguments());
            }

            return implementationType;
        }

        private IList CreateEnumerable(Type dependencyType)
        {
            var implementationList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(dependencyType));
            var implementationsContainers = this._configuration.DependenciesDictionary[dependencyType];
            foreach (var implementationContainer in implementationsContainers)
            {
                var instance = this.ResolveNonIEnumerable(implementationContainer.ImplementationsType,
                    implementationContainer.TimeToLive, dependencyType, implementationContainer.ImplNumber);
                implementationList.Add(instance);
            }

            return implementationList;
        }

        private Implementation GetImplementationsContainerLast(Type dependencyType, ImplementationNumber number)
        {
            if (this._configuration.DependenciesDictionary.ContainsKey(dependencyType))
            {
                return this._configuration.DependenciesDictionary[dependencyType]
                    .FindLast(container => number.HasFlag(container.ImplNumber));
            }

            return null;
        }

        private void AddToSingletons(Type dependencyType, object implementation, ImplementationNumber number)
        {
            if (this._singletons.ContainsKey(dependencyType))
            {
                this._singletons[dependencyType].Add(new SingletonCont(implementation, number));
            }
            else
            {
                this._singletons.Add(dependencyType, new List<SingletonCont>()
                {
                    new SingletonCont(implementation, number)
                });
            }
        }

        private bool IsInSingletons(Type dependencyType, Type implType, ImplementationNumber number)
        {
            var lst = this._singletons.ContainsKey(dependencyType) ? this._singletons[dependencyType] : null;
            return lst?.Find(container => number.HasFlag(container.ImplNumber) && container.Instance.GetType() == implType) is not null;
        }
    }
}
