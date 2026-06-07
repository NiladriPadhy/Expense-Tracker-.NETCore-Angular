using ExpenseTracker.Application.Auth;
using ExpenseTracker.Application.Auth.Dtos;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ExpenseTracker.Application.Tests.Auth;

public class LogoutAndRefreshHandlerTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Logout_KnownToken_RevokesAndSaves()
    {
        var fakes = new HandlerFakes();
        var token = RefreshToken.Issue(Guid.NewGuid(), "refresh-plain-hash", Now.AddDays(7), Now);
        fakes.RefreshTokens.ByHash[token.TokenHash] = token;

        var handler = new LogoutHandler(fakes.RefreshTokens, fakes.Tokens, fakes.Uow, fakes.Clock);
        await handler.HandleAsync("refresh-plain", CancellationToken.None);

        token.RevokedAtUtc.Should().NotBeNull();
        fakes.Uow.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Logout_UnknownToken_DoesNotThrow()
    {
        var fakes = new HandlerFakes();

        var handler = new LogoutHandler(fakes.RefreshTokens, fakes.Tokens, fakes.Uow, fakes.Clock);
        await handler.HandleAsync("ghost", CancellationToken.None);

        fakes.Uow.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Refresh_RotatesToken_AndIssuesAccessToken()
    {
        var fakes = new HandlerFakes();
        var user = User.Create("Jane", "jane@x.com", "+15551234567", "hash:S", "USD", Now);
        fakes.Users.Store[user.Id] = user;

        var existing = RefreshToken.Issue(user.Id, "refresh-plain-hash", Now.AddDays(7), Now);
        fakes.RefreshTokens.ByHash[existing.TokenHash] = existing;

        var handler = new RefreshTokenHandler(fakes.RefreshTokens, fakes.Users, fakes.Tokens, fakes.Uow, fakes.Clock);

        var result = await handler.HandleAsync(new RefreshRequest("refresh-plain"), CancellationToken.None);

        result.AccessToken.Should().Be("access");
        result.RefreshToken.Should().Be("refresh-plain");
        existing.RevokedAtUtc.Should().NotBeNull();
        fakes.RefreshTokens.Added.Should().HaveCount(1);
    }

    [Fact]
    public async Task Refresh_UnknownToken_ThrowsUnauthorized()
    {
        var fakes = new HandlerFakes();
        var handler = new RefreshTokenHandler(fakes.RefreshTokens, fakes.Users, fakes.Tokens, fakes.Uow, fakes.Clock);

        var ex = await handler.Invoking(h => h.HandleAsync(new RefreshRequest("nope"), CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedException>();
        ex.Which.Code.Should().Be("invalid_refresh_token");
    }

    [Fact]
    public async Task Refresh_DeletedUser_ThrowsUnauthorized()
    {
        var fakes = new HandlerFakes();
        var user = User.Create("Jane", "jane@x.com", "+15551234567", "hash:S", "USD", Now);
        user.SoftDelete(Now);
        fakes.Users.Store[user.Id] = user;

        var existing = RefreshToken.Issue(user.Id, "refresh-plain-hash", Now.AddDays(7), Now);
        fakes.RefreshTokens.ByHash[existing.TokenHash] = existing;

        var handler = new RefreshTokenHandler(fakes.RefreshTokens, fakes.Users, fakes.Tokens, fakes.Uow, fakes.Clock);

        await handler.Invoking(h => h.HandleAsync(new RefreshRequest("refresh-plain"), CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedException>();
    }
}
