using System.Data;
using System.Globalization;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Identity;

// An explicit development-only, insert-only seed. No EnsureCreated, migrations,
// identity override, deletion, password reset or updates to existing business rows.
if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Development")
    throw new InvalidOperationException("Seed requires ASPNETCORE_ENVIRONMENT=Development.");
var password = Environment.GetEnvironmentVariable("GYMHUNG_DEMO_PASSWORD");
if (string.IsNullOrWhiteSpace(password) || password.Length < 10)
    throw new InvalidOperationException("Set GYMHUNG_DEMO_PASSWORD with at least 10 characters; it is never printed.");
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__GymDatabase")
    ?? "Server=.\\SQLEXPRESS;Database=gymhung_demo;Trusted_Connection=True;TrustServerCertificate=True";
var builder = new SqlConnectionStringBuilder(connectionString);
if (!builder.InitialCatalog.Equals("gymhung_demo", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Seed is restricted to the existing gymhung_demo database.");
var input = args.FirstOrDefault() ?? Path.Combine(AppContext.BaseDirectory, "seed-data.json");
using var fixture = JsonDocument.Parse(await File.ReadAllTextAsync(input));
await using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();
await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);
await Run("DECLARE @result int; EXEC @result=sp_getapplock @Resource=N'GYMHUNG explicit demo seed', @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=30000; IF @result<0 THROW 51000,'Another demo seed holds the lock; retry later.',1;");
var columns = new Dictionary<string, Dictionary<string, Column>>();
await using (var command = Command("SELECT t.name,c.name,ty.name,c.is_identity,c.is_nullable FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id WHERE SCHEMA_NAME(t.schema_id)='dbo'"))
await using (var reader = await command.ExecuteReaderAsync())
    while (await reader.ReadAsync()) {
        var table = reader.GetString(0);
        if (!columns.ContainsKey(table)) columns[table] = new(StringComparer.OrdinalIgnoreCase);
        var column = new Column(reader.GetString(1), reader.GetString(2), reader.GetBoolean(3), reader.GetBoolean(4));
        columns[table][column.Name] = column;
    }
var foreignKeys = new Dictionary<string, Dictionary<string, string>>();
await using (var command = Command("SELECT OBJECT_NAME(f.parent_object_id), COL_NAME(f.parent_object_id,f.parent_column_id), OBJECT_NAME(f.referenced_object_id) FROM sys.foreign_key_columns f"))
await using (var reader = await command.ExecuteReaderAsync())
    while (await reader.ReadAsync()) {
        var table = reader.GetString(0);
        if (!foreignKeys.ContainsKey(table)) foreignKeys[table] = new(StringComparer.OrdinalIgnoreCase);
        foreignKeys[table][reader.GetString(1)] = reader.GetString(2);
    }
string[] order = ["Roles","Permissions","Users","UserRoles","RolePermissions","Members","Trainers","Staffs","EmergencyContacts","MemberTrainers","Packages","MemberPackages","Payments","PaymentTransactions","Rooms","Exercises","WorkoutPlans","WorkoutPlanDetails","TrainerSchedules","WorkoutSchedules","Bookings","Attendance","CheckIns","WorkoutResults","Progress","EquipmentCategories","Equipment","MaintenanceRecords","Notifications","AIConversations","AIConversationMessages","AIRecommendations","ChurnPredictions","AuditLogs"];
var keys = new Dictionary<string,string[]> {
 ["Roles"]=["RoleName"], ["Permissions"]=["PermissionCode"], ["Users"]=["Username"], ["UserRoles"]=["UserId","RoleId"], ["RolePermissions"]=["RoleId","PermissionId"],
 ["Members"]=["UserId"], ["Trainers"]=["UserId"], ["Staffs"]=["UserId"], ["EmergencyContacts"]=["MemberId","Phone"], ["MemberTrainers"]=["MemberId","TrainerId","Note"],
 ["Packages"]=["PackageName"], ["MemberPackages"]=["MemberId","Notes"], ["Payments"]=["TransactionCode"], ["PaymentTransactions"]=["PaymentId","TransactionCode"],
 ["Rooms"]=["RoomName"], ["Exercises"]=["ExerciseName"], ["WorkoutPlans"]=["MemberId","Note"], ["WorkoutPlanDetails"]=["PlanId","OrderNo"],
 ["TrainerSchedules"]=["TrainerId","Note"], ["WorkoutSchedules"]=["Note"], ["Bookings"]=["MemberId","WorkoutScheduleId"], ["Attendance"]=["BookingId"],
 ["CheckIns"]=["MemberId","Note"], ["WorkoutResults"]=["MemberId","Note"], ["Progress"]=["MemberId","Note"], ["EquipmentCategories"]=["CategoryName"],
 ["Equipment"]=["SerialNumber"], ["MaintenanceRecords"]=["EquipmentId","Description"], ["Notifications"]=["UserId","Title"], ["AIConversations"]=["MemberId","ContextData"],
 ["AIConversationMessages"]=["ConversationId","SenderType"], ["AIRecommendations"]=["MemberId","ReferenceData"], ["ChurnPredictions"]=["MemberId","ModelVersion"], ["AuditLogs"]=["Action","EntityName","NewData"]
};
var maps = new Dictionary<string,Dictionary<string,long>>();
var hasher = new PasswordHasher<object>();
var insertedTotal = 0;
// Reject duplicate synthetic IDs before any INSERT. JSON regenerated tomorrow
// must retain unique source identities independently of SQL identity values.
foreach (var table in order) {
    var identity = columns.GetValueOrDefault(table)?.Values.FirstOrDefault(c => c.Identity);
    if (identity is null) continue;
    var key = char.ToLowerInvariant(identity.Name[0]) + identity.Name[1..];
    var seen = new HashSet<string>();
    foreach (var row in fixture.RootElement.GetProperty(table).EnumerateArray())
        if (!seen.Add(row.GetProperty(key).GetString()!)) throw new InvalidOperationException($"Duplicate source ID in {table}; no data written.");
}
foreach (var table in order) {
    if (!columns.TryGetValue(table, out var tableColumns)) throw new InvalidOperationException($"Required SQL table missing: {table}. Import original SQL manually into an empty database first.");
    var identity = tableColumns.Values.FirstOrDefault(c => c.Identity);
    maps[table] = [];
    var inserted = 0;
    foreach (var source in fixture.RootElement.GetProperty(table).EnumerateArray()) {
        var sourceId = identity is null ? "" : source.GetProperty(char.ToLowerInvariant(identity.Name[0]) + identity.Name[1..]).GetString()!;
        var row = new Dictionary<string,object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in source.EnumerateObject()) {
            if (!tableColumns.TryGetValue(property.Name, out var column) || column.Identity) continue;
            object? value = property.Value.ValueKind switch { JsonValueKind.Null => null, JsonValueKind.True => true, JsonValueKind.False => false, JsonValueKind.Number => property.Value.GetDecimal(), _ => property.Value.GetString() };
            if (value is not null && foreignKeys.TryGetValue(table,out var tableFks) && tableFks.TryGetValue(column.Name,out var target)) {
                var reference = Convert.ToString(value, CultureInfo.InvariantCulture)!;
                if (!maps.TryGetValue(target,out var map) || !map.TryGetValue(reference,out var mapped)) throw new InvalidOperationException($"Unmapped foreign key: {table}.{column.Name} -> {target}:{reference}");
                value = mapped;
            }
            row[column.Name] = value;
        }
        // Stable per-row markers avoid duplicates when dates are regenerated tomorrow.
        string marker = $"GYMHUNG-DEMO-v1:{table}:{sourceId}";
        string? markerColumn = table switch {
            "MemberTrainers" or "WorkoutPlans" or "TrainerSchedules" or "WorkoutSchedules" or "CheckIns" or "WorkoutResults" or "Progress" => "Note",
            "MemberPackages" => "Notes", "MaintenanceRecords" => "Description", "AIConversations" => "ContextData", "AIRecommendations" => "ReferenceData", "AuditLogs" => "NewData", _ => null
        };
        if (markerColumn is not null) row[markerColumn] = marker;
        if (table=="Users") row["PasswordHash"] = hasher.HashPassword(new object(), password);
        if (table=="Payments") row["Provider"] = row.GetValueOrDefault("PaymentMethod")?.ToString()=="Cash" ? "Cash" : "Demo";
        if (table=="AuditLogs") row["EntityId"] = null; // generic EntityId has no FK; never retain fixture identity in SQL.
        var naturalKeys = keys[table];
        var where = string.Join(" AND ",naturalKeys.Select((key,i) => row.GetValueOrDefault(key) is null ? $"[{key}] IS NULL" : $"[{key}]=@k{i}"));
        await using var find = Command($"SELECT TOP(1) {(identity is null ? "CAST(1 AS bigint)" : $"[{identity.Name}]")} FROM [dbo].[{table}] WITH(UPDLOCK,HOLDLOCK) WHERE {where}");
        for(var i=0;i<naturalKeys.Length;i++) if(row.GetValueOrDefault(naturalKeys[i]) is not null) Add(find,$"@k{i}",tableColumns[naturalKeys[i]],row[naturalKeys[i]]);
        var existing = await find.ExecuteScalarAsync();
        long actual;
        if (existing is not null) actual = Convert.ToInt64(existing);
        else {
            var names = row.Keys.ToArray();
            await using var insert = Command($"INSERT [dbo].[{table}] ({string.Join(",",names.Select(n=>$"[{n}]"))}) {(identity is null ? "" : $"OUTPUT INSERTED.[{identity.Name}]")} VALUES ({string.Join(",",names.Select((_,i)=>$"@p{i}"))})");
            for(var i=0;i<names.Length;i++) Add(insert,$"@p{i}",tableColumns[names[i]],row[names[i]]);
            actual = identity is null ? (await insert.ExecuteNonQueryAsync() > 0 ? 1 : 0) : Convert.ToInt64(await insert.ExecuteScalarAsync());
            inserted++; insertedTotal++;
        }
        if (identity is not null) maps[table][sourceId] = actual;
    }
    Console.WriteLine($"{table}: added {inserted}");
}
await transaction.CommitAsync();
Console.WriteLine($"Completed, {insertedTotal} rows inserted. Existing data was preserved.");
SqlCommand Command(string sql) => new(sql,connection,transaction);
async Task Run(string sql) { await using var cmd=Command(sql); await cmd.ExecuteNonQueryAsync(); }
static void Add(SqlCommand command,string name,Column column,object? value) {
    var type = column.Type switch { "bigint"=>SqlDbType.BigInt,"int"=>SqlDbType.Int,"bit"=>SqlDbType.Bit,"decimal"=>SqlDbType.Decimal,"date"=>SqlDbType.Date,"time"=>SqlDbType.Time,"datetime2"=>SqlDbType.DateTime2,_=>SqlDbType.NVarChar };
    var parameter=command.Parameters.Add(name,type);
    if(type==SqlDbType.Decimal) { parameter.Precision=19;parameter.Scale=4; }
    if(type==SqlDbType.NVarChar) parameter.Size=-1;
    if(value is string text && type==SqlDbType.Time) value=TimeSpan.Parse(text,CultureInfo.InvariantCulture);
    else if(value is string date && (type==SqlDbType.Date || type==SqlDbType.DateTime2)) value=DateTime.Parse(date,CultureInfo.InvariantCulture,DateTimeStyles.None);
    parameter.Value=value??DBNull.Value;
}
record Column(string Name,string Type,bool Identity,bool Nullable);
