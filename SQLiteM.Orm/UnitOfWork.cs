using SQLiteM.Abstractions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteM.Orm
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        public IDbConnection Connection { get; }

        public IDbTransaction Transaction { get; private set; }

        private bool _completed;

        public UnitOfWork(IConnectionFactory factory)
        {
            Connection = factory.Create();
            Connection.Open();
            Transaction = Connection.BeginTransaction();
        }

        public Task CommitAsync(CancellationToken ct = default)
        {
            if(_completed) return Task.CompletedTask;
            Transaction.Commit();
            _completed = true;
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken ct = default)
        {
            if (_completed) return Task.CompletedTask;
            Transaction.Rollback();
            _completed = true;
            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            if(!_completed)
            { 
                try
                {
                    await RollbackAsync();
                }
                catch { }
                Transaction.Dispose();
                Connection.Close();
                Connection.Dispose();
            }
        }

    }
}
