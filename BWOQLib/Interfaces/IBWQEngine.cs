using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Rochas.BWOQ
{
    public interface IBWQEngine<T> where T : class
    {
        IQueryable<T> Where(string extExp);

        IQueryable<T> OrderBy(string extExp);

        IQueryable<T> GroupBy(string _byExp, string grpExp);
    }
}
