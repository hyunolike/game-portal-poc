using System.Data;
using GamePortal.Application.Abstractions;
using GamePortal.Application.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GamePortal.Infrastructure.Persistence;

/// <summary>SqlBulkCopy 기반 고유 쿠폰 코드 INSERT. 10만 건 기준 EF SaveChanges 대비 수십 배 빠르다.</summary>
internal sealed class CouponCodeBulkWriter(PortalDbContext db) : ICouponCodeBulkWriter
{
    private const int BatchSize = 5_000;

    public async Task WriteAsync(long campaignId, IReadOnlyCollection<string> codes, CancellationToken cancellationToken)
    {
        using var table = new DataTable();
        table.Columns.Add("CampaignId", typeof(long));
        table.Columns.Add("Code", typeof(string));
        foreach (var code in codes)
        {
            table.Rows.Add(campaignId, code);
        }

        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            var connection = (SqlConnection)db.Database.GetDbConnection();
            var transaction = (SqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction();

            using var bulk = new SqlBulkCopy(connection, SqlBulkCopyOptions.CheckConstraints, transaction);
            bulk.DestinationTableName = "dbo.CouponCodes";
            bulk.BatchSize = BatchSize;
            bulk.BulkCopyTimeout = 120;
            bulk.ColumnMappings.Add("CampaignId", "CampaignId");
            bulk.ColumnMappings.Add("Code", "Code");

            await bulk.WriteToServerAsync(table, cancellationToken);
        }
        catch (SqlException ex) when (SqlErrors.IsUniqueViolation(ex))
        {
            throw new UniqueConstraintViolationException(ex.Message, ex);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}
