using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace InjectionCont
{
    class ConfigValidator
    {
        public bool Validate(DependenciesConfig configuration)
        {
            Stack<Type> nestedTypes = new Stack<Type>();
            return configuration.DependenciesDictionary.Values.
                All(listImplementations => listImplementations.
                    All(implementation => CanBeCreated(implementation.ImplementationsType, configuration, nestedTypes)));
        }

        private bool CanBeCreated(Type instanceType, DependenciesConfig configuration, Stack<Type> nestedTypes)
        {
            nestedTypes.Push(instanceType);
            var constructors = instanceType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            foreach (var constructor in constructors)
            {
                var requiredParams = constructor.GetParameters();
                foreach (var parameter in requiredParams)
                {
                    Type parameterType;
                    if (parameter.ParameterType.ContainsGenericParameters)
                    {
                        parameterType = parameter.ParameterType.GetInterfaces()[0];
                    }
                    else if (parameter.ParameterType.GetInterfaces().Any(i => i.Name == "IEnumerable"))
                    {
                        parameterType = parameter.ParameterType.GetGenericArguments()[0];
                    }
                    else
                    {
                        parameterType = parameter.ParameterType;
                    }

                    if (parameterType.IsInterface && IsInContainer(parameterType, configuration)) continue;
                    nestedTypes.Pop();
                    return false;
                }
            }

            nestedTypes.Pop();
            return true;
        }

        private bool IsInContainer(Type type, DependenciesConfig configuration)
        {
            return configuration.DependenciesDictionary.ContainsKey(type);
        }
    }
}
