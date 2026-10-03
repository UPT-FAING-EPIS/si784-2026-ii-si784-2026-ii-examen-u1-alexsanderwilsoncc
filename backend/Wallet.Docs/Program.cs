using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Wallet.Api.Data;
using Wallet.Api.Services;
using Wallet.Api.Controllers;
var directory = Path.GetFullPath(args.Length > 0 ? args[0] : "docs");
Directory.CreateDirectory(directory);
using var db = new WalletDbContext(new DbContextOptionsBuilder<WalletDbContext>().UseSqlite("Data Source=:memory:").Options);
var model = db.GetServiceModel();
var dictionary = new StringBuilder("# Diccionario de datos\n\nGenerado desde metadatos EF Core. Importes API/C#: decimal; SQLite: INTEGER en centavos. Fechas UTC.\n\n");
var er = new StringBuilder("# Diagrama entidad-relación\n\n```mermaid\nerDiagram\n");
var classes = new StringBuilder("# Diagrama de clases\n\n```mermaid\nclassDiagram\n");
foreach (var entity in model.GetEntityTypes().OrderBy(x => x.ClrType.Name))
{
    var name = entity.ClrType.Name;
    dictionary.Append($"## {name} ({entity.GetTableName()})\n\n| Campo | C# | SQLite | Nulo | Restricción |\n|---|---|---|---|---|\n");
    er.Append($"    {name} {{\n"); classes.Append($"    class {name} {{\n");
    foreach (var property in entity.GetProperties())
    {
        var key = property.IsPrimaryKey() ? "PK" : property.IsForeignKey() ? "FK" : "";
        var rule = property.IsConcurrencyToken ? "Control de concurrencia" : key;
        if (property.GetMaxLength() is int length) rule += $" longitud máxima {length}";
        var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
        if (type.IsEnum) rule += " " + string.Join(", ", Enum.GetNames(type));
        dictionary.Append($"| {property.Name} | {type.Name} | {property.GetColumnType()} | {(property.IsNullable ? "Sí" : "No")} | {rule} |\n");
        er.Append($"        {type.Name} {property.Name}{(key.Length == 0 ? "" : " " + key)}\n");
        classes.Append($"        +{type.Name} {property.Name}\n");
    }
    er.Append("    }\n"); classes.Append("    }\n");
    foreach (var index in entity.GetIndexes())
        dictionary.Append($"\nÍndice {(index.IsUnique ? "único" : "")}: {string.Join(", ", index.Properties.Select(x => x.Name))}.\n");
    foreach (var constraint in entity.GetCheckConstraints())
        dictionary.Append($"\n- {constraint.Name}: `{constraint.Sql}`\n");
    foreach (var fk in entity.GetForeignKeys())
        er.Append($"    {fk.PrincipalEntityType.ClrType.Name} {(fk.IsRequired ? "||" : "|o")}--o{{ {name} : \"{fk.Properties[0].Name}\"\n");
    dictionary.Append("\n");
}
foreach (var type in new[] { typeof(WalletService), typeof(WalletsController), typeof(WalletDbContext) })
{
    classes.Append($"    class {type.Name} {{\n");
    foreach (var method in type.GetMethods(System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).Where(x => !x.IsSpecialName))
        classes.Append($"        +{method.Name}()\n");
    classes.Append("    }\n");
}
classes.Append("    WalletsController --> WalletService\n    WalletService --> WalletDbContext\n    WalletDbContext --> Wallet\n    WalletDbContext --> Transaction\n");
er.Append("```\n"); classes.Append("```\n");
File.WriteAllText(Path.Combine(directory, "data-dictionary.md"), dictionary.ToString().TrimEnd() + "\n");
File.WriteAllText(Path.Combine(directory, "er-diagram.md"), er.ToString());
File.WriteAllText(Path.Combine(directory, "class-diagram.md"), classes.ToString());
Console.WriteLine($"Generated EF documentation in {directory}");
static class ModelAccess
{
    public static IModel GetServiceModel(this WalletDbContext db) =>
     Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>(db).Model;
}
