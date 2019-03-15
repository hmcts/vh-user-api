using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace UserApi.Helper
{
    public static class EnumExtensions
    {
        public static T GetValueByName<T>(this string name)
        {
            var values = from f in typeof(T).GetFields(BindingFlags.Static | BindingFlags.Public)
                         let attribute = Attribute.GetCustomAttribute(f, typeof(DisplayAttribute)) as DisplayAttribute
                         where attribute != null && attribute.Name == name
                         select (T)f.GetValue(null);

            if (values.Any())
            {
                return (T)(object)values.FirstOrDefault();
            }

            return default(T);
        }

        public static bool TryParse<T>(string value, out T? result) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum) throw new ArgumentException("Invalid Enum");

            result = Enum.TryParse(value.GetValueByName<AadGroup>().ToString(), out T tempResult) ? tempResult : default(T?);

            return (result != null);
        }
    }
}
