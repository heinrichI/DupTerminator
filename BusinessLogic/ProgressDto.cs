using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic
{
    public class ProgressDto : IEquatable<ProgressDto>
    {
        public string PhisicalDrive { get; internal set; }
        public string Status { get; internal set; }


        public bool Equals(ProgressDto? other)
        {
            if (other is null)
            {
                return false;
            }

            // Optimization for a common success case.
            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            // If run-time types are not exactly the same, return false.
            if (this.GetType() != other.GetType())
            {
                return false;
            }

            // Return true if the fields match.
            // Note that the base class is not invoked because it is
            // System.Object, which defines Equals as reference equality.
            return (PhisicalDrive == other.PhisicalDrive) && (Status == other.Status);
        }
    }
}
