using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rochas.BWOQ
{
    public class InvalidInternalCombinAttribute : Exception
    {
        internal const string errDesc = "Invalid amount of comparation attributes.";
        public InvalidInternalCombinAttribute()
            : base(errDesc)
        {
        }
    }
}
