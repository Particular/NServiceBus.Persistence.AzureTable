using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;

public static class EqualityComparers
{
    /// <summary>
    /// Gets an <see cref="IEqualityComparer{Object}"/> casting down the passed object to the proper <paramref name="type"/>.
    /// </summary>
    public static IEqualityComparer<object> GetValueComparer(EdmType type)
    {
        switch (type)
        {
            case EdmType.Binary:
                return new EqualityComparer((b1, b2) => Compare((byte[]) b1, (byte[]) b2), o => ((byte[]) o).Length);
            case EdmType.String:
            case EdmType.DateTime:
            case EdmType.Double:
            case EdmType.Guid:
            case EdmType.Int32:
            case EdmType.Int64:
                return new EqualityComparer((o1, o2) => o1.Equals(o2), o => o.GetHashCode());
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    static bool Compare(byte[] a1, byte[] a2)
    {
        if (a1.Length != a2.Length)
        {
            return false;
        }

        // ReSharper disable once LoopCanBeConvertedToQuery
        for (var i = 0; i < a1.Length; i++)
        {
            if (a1[i] != a2[i])
            {
                return false;
            }
        }

        return true;
    }

    sealed class EqualityComparer : IEqualityComparer<object>
    {
        Func<object, object, bool> equals;
        Func<object, int> getHashCode;

        public EqualityComparer(Func<object, object, bool> equals, Func<object, int> getHashCode)
        {
            this.equals = equals;
            this.getHashCode = getHashCode;
        }

        bool IEqualityComparer<object>.Equals(object x, object y)
        {
            return @equals(x, y);
        }

        int IEqualityComparer<object>.GetHashCode(object obj)
        {
            return @getHashCode(obj);
        }
    }
}