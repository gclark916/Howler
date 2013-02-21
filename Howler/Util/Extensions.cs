using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Security.Cryptography;
using Iesi.Collections.Generic;
using NHibernate.Linq;

namespace Howler.Util
{
    public static class Extensions
    {
        public static HashedSet<T> ToHashedSet<T>(this NhQueryable<T> source)
        {
            var result = new HashedSet<T>();
            foreach (T t in source)
            {
                result.Add(t);
            }
            return result;
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
        {
            return new HashSet<T>(source, comparer);
        }

        public static string GetMd5Hash(this MD5 md5Hash, string input)
        {
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            StringBuilder sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2", CultureInfo.InvariantCulture));
            }

            return sBuilder.ToString();
        }

        public static string GetMd5Hash(this MD5 md5Hash, byte[] input)
        {
            byte[] data = md5Hash.ComputeHash(input);

            StringBuilder sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2", CultureInfo.InvariantCulture));
            }

            return sBuilder.ToString();
        }

        public static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }
    }
}
