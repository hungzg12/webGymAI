using GymHung.Application;
using Xunit;

namespace GymHung.Tests;

public sealed class PackageUsageTests
{
    [Fact] public void Unlimited_period_pass_remains_unlimited() => Assert.Null(PackageUsageService.AfterUse(null));
    [Fact] public void Last_visit_cannot_go_negative()
    {
        Assert.Equal(0, PackageUsageService.AfterUse(1));
        Assert.Throws<ApiException>(() => PackageUsageService.AfterUse(0));
        Assert.Throws<ApiException>(() => PackageUsageService.AfterUse(-1));
    }
    [Theory]
    [InlineData("PERSONAL TRAINING", true)]
    [InlineData("Gói huấn luyện cá nhân", true)]
    [InlineData("GYM BASIC", false)]
    [InlineData("DAY PASS", false)]
    public void Pt_is_separate_from_admission(string name, bool trainer) => Assert.Equal(trainer, PackageUsageService.IsTrainerPackage(name));
}
