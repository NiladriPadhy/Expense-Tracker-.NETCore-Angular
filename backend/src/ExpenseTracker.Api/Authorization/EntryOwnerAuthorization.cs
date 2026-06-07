using System.Security.Claims;
using ExpenseTracker.Domain.Common;
using Microsoft.AspNetCore.Authorization;

namespace ExpenseTracker.Api.Authorization;

public sealed class EntryOwnerRequirement : IAuthorizationRequirement
{
    public Guid EntryOwnerUserId { get; }
    public EntryOwnerRequirement(Guid ownerUserId) => EntryOwnerUserId = ownerUserId;
}

public sealed class EntryOwnerAuthorizationHandler : AuthorizationHandler<EntryOwnerRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, EntryOwnerRequirement requirement)
    {
        var roleClaim = context.User.FindFirstValue("role");
        if (string.Equals(roleClaim, UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
        var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? context.User.FindFirstValue("sub");
        if (Guid.TryParse(sub, out var userId) && userId == requirement.EntryOwnerUserId)
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
