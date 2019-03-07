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

            if (values.Count() > 0)
            {
                return (T)(object)values.FirstOrDefault();
            }

            return default(T);
        }
    }
}
