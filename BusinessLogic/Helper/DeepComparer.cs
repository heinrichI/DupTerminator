using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Helper
{
    public static class DeepComparer
    {
        public static bool DeepEquals<T>(T obj1, T obj2)
        {
            if (ReferenceEquals(obj1, obj2))
                return true;
            if (obj1 == null || obj2 == null)
                return false;
            if (obj1.GetType() != obj2.GetType())
                return false;

            foreach (var property in typeof(T).GetProperties())
            {
                var value1 = property.GetValue(obj1);
                var value2 = property.GetValue(obj2);

                if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                {
                    if (!DeepEquals(value1, value2))
                        return false;
                }
                else
                {
                    if (!Equals(value1, value2))
                        return false;
                }
            }
            return true;
        }
    }
}
