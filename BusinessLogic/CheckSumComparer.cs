using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.BusinessLogic
{
    internal class CheckSumComparer : IEqualityComparer<ExtendedFileInfo>
    {
        public bool Equals(ExtendedFileInfo x, ExtendedFileInfo y)
        {
            //Check whether the compared objects reference the same data.
            if (object.ReferenceEquals(x, y)) return true;

            //Check whether any of the compared objects is null.
            if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null))
                return false;

            return x.CheckSum == y.CheckSum;
        }

        // If Equals() returns true for a pair of objects
        // then GetHashCode() must return the same value for these objects.

        public int GetHashCode([DisallowNull] ExtendedFileInfo obj)
        {
            //Check whether the object is null
            if (object.ReferenceEquals(obj, null)) return 0;

            int hash = obj.CheckSum == null ? 0 : obj.CheckSum.GetHashCode();

            return hash;
        }
    }
}
