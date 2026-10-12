using GymHung.Application;
using Microsoft.AspNetCore.Authorization;
namespace GymManagement.Api;

public sealed record DatabasePermission(string Code) : IAuthorizationRequirement;
public sealed class DatabasePermissionHandler(AccessService access) : AuthorizationHandler<DatabasePermission>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, DatabasePermission requirement)
    {
        if (await access.HasAsync(context.User, requirement.Code, CancellationToken.None))
            context.Succeed(requirement);
    }
}
