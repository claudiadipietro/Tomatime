using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tomatime.Api.DTOs;
using Tomatime.Api.Models;
using Tomatime.Api.Services;

namespace Tomatime.Api.Tests;

public class PasswordResetEmailTests
{
    [Fact]
    public async Task SendsMessageThroughGmailTransport()
    {
        var transport = new FakeTransport();
        var sender = new PasswordResetEmailSender(Config(new()
        {
            ["Gmail:Address"] = "sender@gmail.com",
            ["Gmail:AppPassword"] = "app-password",
        }), transport);

        await sender.SendAsync("recipient@example.com", "https://example.com/reset-password?token=secret");

        Assert.Equal("sender@gmail.com", transport.From);
        Assert.Equal("recipient@example.com", transport.To);
        Assert.Equal("sender@gmail.com", transport.Username);
        Assert.Equal("app-password", transport.Password);
        Assert.Contains("https://example.com/reset-password?token=secret", transport.Body);
        Assert.Contains("30 minutos", transport.Body);
    }

    [Fact]
    public async Task MissingCredentialsAndSmtpErrorsAreReportedWithoutSensitiveDetails()
    {
        var transport = new FakeTransport();
        var missing = new PasswordResetEmailSender(Config(new()), transport);
        await Assert.ThrowsAsync<PasswordResetEmailDeliveryException>(
            () => missing.SendAsync("recipient@example.com", "https://example.com/?token=secret"));
        Assert.Null(transport.From);

        transport.Fail = true;
        var sender = new PasswordResetEmailSender(Config(new()
        {
            ["Gmail:Address"] = "sender@gmail.com",
            ["Gmail:AppPassword"] = "app-password",
        }), transport);
        var exception = await Assert.ThrowsAsync<PasswordResetEmailDeliveryException>(
            () => sender.SendAsync("recipient@example.com", "https://example.com/?token=secret"));
        Assert.DoesNotContain("secret", exception.ToString());
        Assert.DoesNotContain("recipient@example.com", exception.ToString());
    }

    [Fact]
    public async Task ExistingUserReceivesLinkAndOnlyTokenHashIsStored()
    {
        var users = new FakeUsers { User = new User { Id = 7, Email = "person@example.com" } };
        var tokens = new FakeTokens();
        var queue = new PasswordResetEmailQueue();
        var service = CreateService(users, tokens, queue, Config(new()), "Development", new FakeLogger());
        var before = DateTimeOffset.UtcNow;

        await service.RequestPasswordResetAsync(new ForgotPasswordRequest(" PERSON@example.com "));

        await using var reader = queue.ReadAllAsync(CancellationToken.None).GetAsyncEnumerator();
        Assert.True(await reader.MoveNextAsync());
        Assert.Equal("person@example.com", reader.Current.Address);
        var uri = new Uri(reader.Current.ResetUrl);
        Assert.Equal("http://localhost:4200/reset-password", uri.GetLeftPart(UriPartial.Path));
        var token = Uri.UnescapeDataString(uri.Query["?token=".Length..]);
        var saved = Assert.Single(tokens.Items);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))), saved.TokenHash);
        Assert.Equal(7, saved.UserId);
        Assert.InRange(saved.ExpiresAt, before.AddMinutes(30), DateTimeOffset.UtcNow.AddMinutes(30));
        Assert.True(tokens.Saved);
    }

    [Fact]
    public async Task UnknownUserAndFullQueueDoNotRevealAccountExistence()
    {
        var users = new FakeUsers();
        var tokens = new FakeTokens();
        var queue = new PasswordResetEmailQueue();
        var logger = new FakeLogger();
        var service = CreateService(users, tokens, queue, Config(new()), "Development", logger);
        await service.RequestPasswordResetAsync(new ForgotPasswordRequest("unknown@example.com"));
        Assert.Empty(tokens.Items);

        for (var i = 0; i < 100; i++) Assert.True(queue.TryEnqueue(new PasswordResetEmail("test@example.com", "url")));
        users.User = new User { Id = 1, Email = "known@example.com" };
        await service.RequestPasswordResetAsync(new ForgotPasswordRequest("known@example.com"));
        Assert.Single(tokens.Items);
        Assert.Contains(logger.Messages, message => message.Contains("queue is full"));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("known@example.com") || message.Contains("token="));
    }

    [Fact]
    public async Task WorkerSendsQueuedMessagesAndLogsDeliveryFailure()
    {
        var queue = new PasswordResetEmailQueue();
        var sender = new FakeSender();
        using var services = new ServiceCollection().AddSingleton<IPasswordResetEmailSender>(sender).BuildServiceProvider();
        var logger = new WorkerLogger();
        using var worker = new PasswordResetEmailWorker(queue, services.GetRequiredService<IServiceScopeFactory>(), logger);
        await worker.StartAsync(CancellationToken.None);
        Assert.True(queue.TryEnqueue(new PasswordResetEmail("known@example.com", "https://example.com/?token=secret")));
        await sender.Called.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await logger.Logged.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await worker.StopAsync(CancellationToken.None);
        Assert.Equal("known@example.com", sender.Email);
        Assert.Equal("https://example.com/?token=secret", sender.Url);
        Assert.DoesNotContain("known@example.com", logger.Message);
        Assert.DoesNotContain("secret", logger.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("http://example.com/reset-password")]
    [InlineData("https://localhost/reset-password")]
    [InlineData("https://example.com/reset-password?token=old")]
    public async Task ProductionRejectsInvalidFrontendUrlForEveryAccount(string? url)
    {
        var users = new FakeUsers();
        var service = CreateService(users, new FakeTokens(), new PasswordResetEmailQueue(),
            Config(new() { ["Frontend:ResetPasswordUrl"] = url }), "Production", new FakeLogger());
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RequestPasswordResetAsync(new ForgotPasswordRequest("unknown@example.com")));
        Assert.False(users.LookupCalled);
    }

    [Fact]
    public async Task ProductionUsesConfiguredHttpsUrl()
    {
        var queue = new PasswordResetEmailQueue();
        var service = CreateService(new FakeUsers { User = new User { Id = 1, Email = "person@example.com" } },
            new FakeTokens(), queue, Config(new() { ["Frontend:ResetPasswordUrl"] = "https://example.com/reset-password" }),
            "Production", new FakeLogger());
        await service.RequestPasswordResetAsync(new ForgotPasswordRequest("person@example.com"));
        await using var reader = queue.ReadAllAsync(CancellationToken.None).GetAsyncEnumerator();
        Assert.True(await reader.MoveNextAsync());
        Assert.StartsWith("https://example.com/reset-password?token=", reader.Current.ResetUrl);
    }

    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static AuthService CreateService(FakeUsers users, FakeTokens tokens, PasswordResetEmailQueue queue,
        IConfiguration config, string environment, FakeLogger logger) =>
        new(users, tokens, new PasswordHasher<User>(), queue, config, new FakeEnvironment { EnvironmentName = environment }, logger);

    private sealed class FakeTransport : IGmailTransport
    {
        public string? From, To, Username, Password, Body;
        public bool Fail;
        public Task SendAsync(MailMessage message, string address, string password, CancellationToken cancellationToken)
        {
            if (Fail) throw new SmtpException("recipient@example.com secret");
            From = message.From?.Address;
            To = message.To.Single().Address;
            Username = address;
            Password = password;
            Body = message.Body;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSender : IPasswordResetEmailSender
    {
        public string? Email, Url;
        public TaskCompletionSource Called = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task SendAsync(string email, string resetUrl, CancellationToken cancellationToken = default)
        {
            Email = email;
            Url = resetUrl;
            Called.TrySetResult();
            throw new PasswordResetEmailDeliveryException("Gmail delivery failed.");
        }
    }

    private sealed class FakeUsers : IUserDatabaseService
    {
        public User? User;
        public bool LookupCalled;
        public Task<User?> GetUserByEmailAsync(string email) { LookupCalled = true; return Task.FromResult(User); }
        public Task<User?> GetUserByIdAsync(int userId) => throw new NotImplementedException();
        public Task<User?> CreateUserAsync(User user) => throw new NotImplementedException();
        public Task<User?> UpdateUserAsync(User user) => throw new NotImplementedException();
        public Task<bool> DeleteUserAsync(int userId) => throw new NotImplementedException();
    }

    private sealed class FakeTokens : IRepository<PasswordResetToken>
    {
        public List<PasswordResetToken> Items = [];
        public bool Saved;
        public IQueryable<PasswordResetToken> Query() => Items.AsQueryable();
        public Task<PasswordResetToken?> GetByIdAsync(object id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAsync(PasswordResetToken entity, CancellationToken cancellationToken = default) { Items.Add(entity); return Task.CompletedTask; }
        public void Update(PasswordResetToken entity) => throw new NotImplementedException();
        public void Remove(PasswordResetToken entity) => throw new NotImplementedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { Saved = true; return Task.FromResult(1); }
    }

    private sealed class FakeEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "";
        public string ApplicationName { get; set; } = "";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FakeLogger : ILogger<AuthService>
    {
        public List<string> Messages = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }

    private sealed class WorkerLogger : ILogger<PasswordResetEmailWorker>
    {
        public string Message = "";
        public TaskCompletionSource Logged = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Message = formatter(state, exception);
            Logged.TrySetResult();
        }
    }
}
