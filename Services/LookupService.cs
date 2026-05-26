using Microsoft.EntityFrameworkCore;
using SGA.Data;
using SGA.DTO;
using SGA.Models;
using System.Data.Common;

namespace SGA.Services
{
    public class LookupService
    {
        private readonly SgaContext _context;

        public LookupService(SgaContext context)
        {
            _context = context;
        }

        public async Task<List<PartNumberLookupRow>> GetPartNumberRowsByReturnFlagAsync(bool isReturnMaterial, string? partNumber = null)
        {
            var connection = _context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;
            if (shouldClose)
                await connection.OpenAsync();

            try
            {
                var hasColumn = await HasIsReturnMaterialColumnAsync(connection);
                if (!hasColumn)
                    return new List<PartNumberLookupRow>();

                var sql = @"SELECT [PartNumberKey], [PartNumberName], [PartNumberNameGDI], [PartNumber], [PartNumberProgram]
FROM [dbo].[PartNumber]
WHERE " + (isReturnMaterial ? "[IsReturnMaterial] = 1" : "([IsReturnMaterial] = 0 OR [IsReturnMaterial] IS NULL)");

                if (!string.IsNullOrWhiteSpace(partNumber))
                    sql += " AND LTRIM(RTRIM([PartNumber])) = @partNumber";

                using var command = connection.CreateCommand();
                command.CommandText = sql;

                if (!string.IsNullOrWhiteSpace(partNumber))
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@partNumber";
                    parameter.Value = partNumber.Trim();
                    command.Parameters.Add(parameter);
                }

                var result = new List<PartNumberLookupRow>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new PartNumberLookupRow
                    {
                        PartNumberKey = reader["PartNumberKey"] as string,
                        PartNumberName = reader["PartNumberName"] as string,
                        PartNumberNameGdi = reader["PartNumberNameGDI"] as string,
                        PartNumber1 = reader["PartNumber"] as string,
                        PartNumberProgram = reader["PartNumberProgram"] as string
                    });
                }

                return result;
            }
            finally
            {
                if (shouldClose)
                    await connection.CloseAsync();
            }
        }

        public async Task<PartNumberLookupRow?> GetPartNumberAsync(string? partNumber, bool isReturnMaterial)
        {
            if (string.IsNullOrWhiteSpace(partNumber))
                return null;

            var rows = await GetPartNumberRowsByReturnFlagAsync(isReturnMaterial, partNumber.Trim());
            return rows.FirstOrDefault();
        }

        public async Task<List<string>> GetPartNumberListAsync(bool isReturnMaterial)
        {
            return (await GetPartNumberRowsByReturnFlagAsync(isReturnMaterial))
                .Select(p => p.PartNumber1)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList()!;
        }

        public async Task<List<string>> GetPartNumberFieldListAsync(bool isReturnMaterial, Func<PartNumberLookupRow, string?> selector)
        {
            return (await GetPartNumberRowsByReturnFlagAsync(isReturnMaterial))
                .Select(selector)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .Cast<string>()
                .ToList();
        }

        public async Task<Area?> GetAreaAsync(string? areaName)
        {
            if (string.IsNullOrWhiteSpace(areaName))
                return null;
            return await _context.Areas.FirstOrDefaultAsync(a => a.AreaName == areaName);
        }

        public async Task<List<string>> GetAreaNameListAsync()
        {
            return (await _context.Areas.Select(a => a.AreaName).ToListAsync())
                .Where(x => x != null)
                .Cast<string>()
                .ToList();
        }

        public async Task<List<string>> GetAreaDescriptionListAsync()
        {
            return (await _context.Areas.Select(a => a.AreaDescription).ToListAsync())
                .Where(x => x != null)
                .Cast<string>()
                .ToList();
        }

        public async Task<StorageType?> GetStorageAsync(string? storageName)
        {
            if (string.IsNullOrWhiteSpace(storageName))
                return null;
            return await _context.StorageTypes.FirstOrDefaultAsync(s => s.StorageName == storageName);
        }

        public async Task<List<string>> GetStorageNameListAsync()
        {
            return (await _context.StorageTypes.Select(s => s.StorageName).ToListAsync())
                .Where(x => x != null)
                .Cast<string>()
                .ToList();
        }

        public async Task<IQueryable<NonHazardou>> BuildBaseQueryAsync(bool isReturnMaterial)
        {
            var partNumbers = await GetPartNumberListAsync(isReturnMaterial);
            if (partNumbers.Count == 0)
                return _context.NonHazardous.Where(x => false);

            return _context.NonHazardous.Where(x => x.PartNumber != null && partNumbers.Contains(x.PartNumber));
        }

        private static async Task<bool> HasIsReturnMaterialColumnAsync(DbConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT COUNT(1)
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = 'PartNumber'
  AND COLUMN_NAME = 'IsReturnMaterial'";

            var count = (int)(await command.ExecuteScalarAsync() ?? 0);
            return count > 0;
        }
    }
}
