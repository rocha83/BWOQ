using System;
using System.Reflection;
using Rochas.SqlWrapper.Helpers;

namespace Rochas.BWOQ.Helpers
{
    public static class Reflector
    {
        public static void InitNullComposition(object sourceObj)
        {
            EntityReflector.InitNullComposition(sourceObj);
        }

        public static void CloneObjectData(object source, object destination)
        {
            EntityReflector.CloneObjectData(source, destination);
        }

        public static PropertyInfo[] GetObjectProps(object source, params object[] filter)
        {
            return EntityReflector.GetObjectProps(source, filter);
        }

        public static object[] GetObjectPropValues(object source, PropertyInfo[] properties)
        {
            return EntityReflector.GetObjectPropValues(source, properties);
        }

        public static object GetTypedValue(Type propType, object propValue)
        {
            return EntityReflector.GetTypedValue(propType, propValue);
        }
    }

    public static class Reflector<T>
    {
        public static T CloneObjectData(object source)
        {
            return EntityReflector.CloneObjectData<T>(source);
        }
    }
}