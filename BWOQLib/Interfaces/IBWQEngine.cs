using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Rochas.BWOQ
{
    public interface IBWQEngine<T> where T : class
    {
        IQueryable Where(string extExp);

        IQueryable OrderBy(string extExp);

        IQueryable GroupBy(string _byExp, string grpExp);
    }
}
