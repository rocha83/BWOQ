using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace System.Linq.Dynamic.BitWise
{
    public interface IBitWiseQuery
    {
        // Métodos longos (nomes completos)
        IQueryable Query(string extExp, bool standAlone);
        IQueryable Where(string extExp);
        IQueryable OrderBy(string extExp);
        IQueryable OrderByDescending(string extExp);
        IQueryable GroupBy(string _byExpr, string grpExp);

        // Métodos curtos (aliases) - retornam IQueryable
        IQueryable Q(string bwqExpr);
        IQueryable W(string extExpr);
        IQueryable O(string extExpr);
        IQueryable OD(string extExpr);
        IQueryable G(string grpExpr, string byExpr);
    }
}
