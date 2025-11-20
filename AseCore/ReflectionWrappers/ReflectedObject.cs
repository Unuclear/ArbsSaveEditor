using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AseCore.ReflectionWrappers
{
    public class ReflectedObject
    {
        protected ReflectedObject(object instance)
        {
            Instance = instance ?? throw new ArgumentNullException(nameof(instance));
            Type = instance.GetType();
        }

        protected internal object Instance { get; }
        protected internal Type Type { get; }

        private FieldInfo GetFieldInfo(string name)
        {
            return Type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private PropertyInfo GetPropertyInfo(string name)
        {
            return Type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        public object Get(string fieldName)
        {
            var fieldInfo = GetFieldInfo(fieldName);
            if (fieldInfo != null) return fieldInfo.GetValue(Instance);

            var propertyInfo = GetPropertyInfo(fieldName);
            if (propertyInfo != null) return propertyInfo.GetValue(Instance);

            throw new MissingMemberException(fieldName, fieldName);
        }

        protected ReflectedObject GetAsReflectedObject(string fieldName)
        {
            return new ReflectedObject(Get(fieldName));
        }

        public void Set(string fieldName, object value)
        {
            var fieldInfo = GetFieldInfo(fieldName);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(Instance, value);
                return;
            }

            var propertyInfo = GetPropertyInfo(fieldName);
            if (propertyInfo != null) propertyInfo.SetValue(Instance, value);
            else throw new MissingMemberException(fieldName, fieldName);
        }

        public override string ToString()
        {
            var sb = new StringBuilder($"{Type.FullName}( ");
            sb.Append(Type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Aggregate("", (current, field) => current + $"{field.Name} = {field.GetValue(Instance)}, "));
            sb.Append(")");
            return sb.ToString();
        }
    }
}