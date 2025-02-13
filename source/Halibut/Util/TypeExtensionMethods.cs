﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Halibut.Util
{

    public static class TypeExtensionMethods
    {
        public static IEnumerable<MethodInfo> GetHalibutServiceMethods(this Type serviceType)
        {
            var methods = serviceType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            foreach (var method in methods.Where(x => !(x.IsSpecialName || x.DeclaringType == typeof(object))))
            {
                yield return method;
            }
        }

        public static bool AllowedOnHalibutInterface(this Type type)
        {
            return type != typeof(object) && type != typeof(Task) && type != typeof(Task<>);
        }
    }
}