using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rochas.BWOQ
{
    public class InvalidCriterCombinAttribute : Exception
    {
        internal const string errDesc = "Invalid use of String criteria attribute with majority operators.";
        public InvalidCriterCombinAttribute()
            : base(errDesc)
        {
        }
    }
}
