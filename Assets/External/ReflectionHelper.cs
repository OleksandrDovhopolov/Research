using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CsvLoader.Editor
{
    public static class ReflectionHelper
    {
        public const BindingFlags InstanceFieldsAccess = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static;
        
        private static IEnumerable<Type> GetAssemblyTypes() => System.AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes());

        private static List<Type> GetAllDerived<T>() where T : class
        {
            return GetAllDerived(typeof(T));
        }
        
        private static List<Type> GetAllDerived(Type derivedType)
        {
            return GetAssemblyTypes()
                .Where(t =>
                    t != derivedType &&
                    derivedType.IsAssignableFrom(t)
                ).ToList();
        }
        
        public static List<Type> GetAllAssignable(Type searchType)
        {
            var result = GetAllDerived(searchType);

            if (!searchType.IsAbstract && searchType.GetConstructor(Type.EmptyTypes) != null)
            {
                result.Insert(0, searchType);
            }
            
            return result;
        }
        
        public static List<T> GetAllDerivedInstances<T>() where T : class
        {
            var derivedTypes = GetAllDerived<T>();
            
            var resut = new List<T>();

            foreach (var derivedType in derivedTypes)
            {
                resut.Add((T)Activator.CreateInstance(derivedType));
            }
            
            return resut;
        }

        public static Type FindType(string typeName) => GetAssemblyTypes().FirstOrDefault(type => type.Name == typeName);
    }
}