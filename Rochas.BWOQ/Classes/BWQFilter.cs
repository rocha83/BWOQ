using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Rochas.BWOQ
{
    public class BWQFilter<T> : IBWQEngine<T>, IQueryable<T>, IEnumerable<T> where T : class
    {
        private IQueryable<T> _source;
        private IQueryable<T> _lastResult;
        private string bwqExpression;
        private BitWiseQuery<T> qryEngine;

        public BWQFilter(IQueryable<T> obj, string extExp)
        {
            _source = obj;
            bwqExpression = extExp;
            qryEngine = new BitWiseQuery<T>(ref _source, ref bwqExpression, this);
            _lastResult = _source;
        }

        public IQueryable<T> Where(string extExpr)
        {
            _lastResult = qryEngine.Where(extExpr) as IQueryable<T>;
            return _lastResult;
        }

        public BWQFilter<T> Where(string extExpr, bool hasSufix)
        {
            var result = qryEngine.Where(extExpr, hasSufix);

            _source = result._source;
            bwqExpression = result.bwqExpression;
            qryEngine = result.qryEngine;
            _lastResult = result._lastResult;

            return this;
        }

        public IQueryable<T> OrderBy(string extExpr)
        {
            _lastResult = qryEngine.OrderBy(extExpr) as IQueryable<T>;
            return _lastResult;
        }

        public IQueryable<T> OrderByDescending(string extExpr)
        {
            _lastResult = qryEngine.OrderByDescending(extExpr) as IQueryable<T>;
            return _lastResult;
        }

        public IQueryable<T> GroupBy(string grpExpr, string extExpr)
        {
            _lastResult = qryEngine.GroupBy(grpExpr, extExpr) as IQueryable<T>;
            return _lastResult;
        }

        // Builder pattern
        public BWQFilter<T> W(string extExpr) { Where(extExpr, true); return this; }
        public BWQFilter<T> O(string extExpr) { OrderBy(extExpr); return this; }
        public BWQFilter<T> OD(string extExpr) { OrderByDescending(extExpr); return this; }
        public IQueryable G(string grpExpr, string byExpr) { return qryEngine.GroupBy(grpExpr, byExpr); }

        // IQueryable<T> implementation
        public Type ElementType => (_lastResult ?? _source).ElementType;
        public Expression Expression => (_lastResult ?? _source).Expression;
        public IQueryProvider Provider => (_lastResult ?? _source).Provider;
        public IEnumerator<T> GetEnumerator() => (_lastResult ?? _source).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
