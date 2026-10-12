using System.Text.Json;
using GymHung.Application;
using GymManagement.Api.Data;
using GymManagement.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace GymHung.Tests;

public sealed class ContractsAndSchemaTests
{
    private static GymDbContext Context() => new(new DbContextOptionsBuilder<GymDbContext>().UseSqlServer("Server=(local);Database=gymhung_demo;Integrated Security=True;TrustServerCertificate=True").Options);
    [Fact]
    public void Bigint_and_money_survive_exact_json_roundtrip()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new LongStringConverter());
        options.Converters.Add(new DecimalStringConverter());
        var original = new FinancialRecord(long.MaxValue, 999999999999999.9999m);
        var json = JsonSerializer.Serialize(original, options);
        Assert.Contains("\"9223372036854775807\"", json);
        Assert.Contains("\"999999999999999.9999\"", json);
        Assert.Equal(original, JsonSerializer.Deserialize<FinancialRecord>(json, options));
    }
    [Fact]
    public void Schema_has_34_tables_and_40_foreign_keys_without_cascade()
    {
        using var db = Context();
        var types = db.Model.GetEntityTypes().ToList();
        Assert.Equal(34, types.Count);
        Assert.Equal(40, types.Sum(x => x.GetForeignKeys().Count()));
        Assert.All(types.SelectMany(x => x.GetForeignKeys()), fk => Assert.NotEqual(DeleteBehavior.Cascade, fk.DeleteBehavior));
    }
    [Fact]
    public void Mapping_preserves_compound_keys_and_schedule_plan_relationship()
    {
        using var db = Context();
        Assert.Equal(new[] { "UserId", "RoleId" }, db.Model.FindEntityType(typeof(UserRole))!.FindPrimaryKey()!.Properties.Select(x => x.Name));
        Assert.Equal(new[] { "RoleId", "PermissionId" }, db.Model.FindEntityType(typeof(RolePermission))!.FindPrimaryKey()!.Properties.Select(x => x.Name));
        Assert.Contains(db.Model.FindEntityType(typeof(WorkoutSchedule))!.GetForeignKeys(), fk => fk.PrincipalEntityType.ClrType == typeof(WorkoutPlan) && fk.Properties.Single().Name == "PlanId");
        Assert.Equal("decimal(19,4)", db.Model.FindEntityType(typeof(Payment))!.FindProperty("Amount")!.GetColumnType()!.Replace(" ", ""));
    }
    [Fact]
    public void Dto_contains_only_scalar_fields_never_password_hash_or_navigations()
    {
        using var db = Context();
        var user = new User { UserId = 9007199254740993, Username = "member", PasswordHash = "secret", Email = "member@example.com", FullName = "Member", Status = "Active" };
        var dto = ResourceDto.From(user, db.Model.FindEntityType(typeof(User))!);
        Assert.Equal("9007199254740993", dto["userId"]);
        Assert.DoesNotContain("passwordHash", dto.Keys);
        Assert.DoesNotContain("userRoles", dto.Keys);
    }
    [Theory]
    [InlineData("DEMO-order-123", 10, 10, true)]
    [InlineData("REAL-order-123", 10, 10, false)]
    [InlineData("DEMO-order-123", 10, 11, false)]
    public void Demo_provider_explicitly_rejects_amount_mismatch_and_real_codes(string code, int expected, int received, bool valid)
    {
        var adapter = new DemoPaymentProvider();
        Assert.Equal("Demo", adapter.Mode);
        Assert.Equal(valid, adapter.Verify(expected, received, code));
    }
    [Fact]
    public void Vietnam_datetime_has_no_implicit_utc_suffix()
    {
        Assert.Equal(DateTimeKind.Unspecified, GymClock.Now.Kind);
        Assert.InRange((GymClock.Now - DateTime.UtcNow).TotalHours, 6.99, 7.01);
    }
    private sealed record FinancialRecord(long Id, decimal Amount);
}

