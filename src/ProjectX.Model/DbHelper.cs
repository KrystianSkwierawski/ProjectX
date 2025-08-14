using System.Transactions;

namespace ProjectX.Model;
public static class DbHelper
{
    public static TransactionScope CreateTransactionScope() => new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
            }, TransactionScopeAsyncFlowOption.Enabled);
}
