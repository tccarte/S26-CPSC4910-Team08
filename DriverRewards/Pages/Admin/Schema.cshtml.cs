using DriverRewards.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Admin;

[Authorize(Roles = "Admin")]
public class SchemaModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public SchemaModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<TableInfo> Tables { get; private set; } = new();

    public void OnGet()
    {
        foreach (var entityType in _context.Model.GetEntityTypes().OrderBy(e => e.GetTableName()))
        {
            var tableName = entityType.GetTableName() ?? entityType.ClrType.Name;

            var columns = entityType.GetProperties()
                .Select(p => new ColumnInfo
                {
                    ColumnName = p.GetColumnName() ?? p.Name,
                    ClrType = GetFriendlyTypeName(p.ClrType),
                    IsNullable = p.IsNullable,
                    IsPrimaryKey = p.IsPrimaryKey(),
                    MaxLength = p.GetMaxLength()
                })
                .OrderBy(c => c.IsPrimaryKey ? 0 : 1)
                .ThenBy(c => c.ColumnName)
                .ToList();

            Tables.Add(new TableInfo { TableName = tableName, Columns = columns });
        }
    }

    private static string GetFriendlyTypeName(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        var baseType = underlying ?? type;

        var name = baseType.Name switch
        {
            "String"   => "string",
            "Int32"    => "int",
            "Int64"    => "long",
            "Boolean"  => "bool",
            "DateTime" => "DateTime",
            "Decimal"  => "decimal",
            "Single"   => "float",
            "Double"   => "double",
            _          => baseType.Name
        };

        return underlying != null ? $"{name}?" : name;
    }

    public class TableInfo
    {
        public string TableName { get; set; } = string.Empty;
        public List<ColumnInfo> Columns { get; set; } = new();
    }

    public class ColumnInfo
    {
        public string ColumnName { get; set; } = string.Empty;
        public string ClrType { get; set; } = string.Empty;
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public int? MaxLength { get; set; }
    }
}
