using ChatPortal.Data;
using ChatPortal.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatPortal.Services;

public interface ICreditService
{
    Task<int> GetBalanceAsync(int userId);
    Task<bool> DeductCreditsAsync(int userId, int amount, string description, int? dataSourceId = null);
    Task AddCreditsAsync(int userId, int amount, string transactionType, string description);
    Task<List<CreditTransaction>> GetTransactionHistoryAsync(int userId, int pageSize = 20);
}

public class CreditService : ICreditService
{
    private readonly AppDbContext _db;

    public CreditService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> GetBalanceAsync(int userId)
    {
        return await _db.CreditTransactions
            .Where(ct => ct.UserId == userId)
            .SumAsync(ct => ct.Amount);
    }

    public async Task<bool> DeductCreditsAsync(int userId, int amount, string description, int? dataSourceId = null)
    {
        var balance = await GetBalanceAsync(userId);
        if (balance < amount)
            return false;

        _db.CreditTransactions.Add(new CreditTransaction
        {
            UserId = userId,
            Amount = -amount,
            TransactionType = "QueryDeduction",
            Description = description,
            DataSourceId = dataSourceId
        });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task AddCreditsAsync(int userId, int amount, string transactionType, string description)
    {
        _db.CreditTransactions.Add(new CreditTransaction
        {
            UserId = userId,
            Amount = amount,
            TransactionType = transactionType,
            Description = description
        });
        await _db.SaveChangesAsync();
    }

    public async Task<List<CreditTransaction>> GetTransactionHistoryAsync(int userId, int pageSize = 20)
    {
        return await _db.CreditTransactions
            .Where(ct => ct.UserId == userId)
            .OrderByDescending(ct => ct.CreatedAt)
            .Take(pageSize)
            .ToListAsync();
    }
}
