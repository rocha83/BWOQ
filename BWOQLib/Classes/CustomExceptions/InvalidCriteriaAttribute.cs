using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rochas.BWOQ
{
    public class InvalidCriteriaAttribute : Exception
    {
        internal const string errDesc = "Invalid use of Numeric, DateTime, Null criteria or Internal Comparation without Comparation operators.";
        public InvalidCriteriaAttribute()
            : base(errDesc)
        {
        }
    }
}
