using FluentAssertions;
using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Application.Security;
using invoice_v1.src.Application.Services;
using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Invoice_v1.UnitTests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IJwtTokenService> _jwtMock = new();
    private readonly Mock<ILogger<AuthService>> _loggerMock = new();

    private AuthService CreateService()
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            AccessTokenMinutes = 60
        });

        return new AuthService(
            _userRepoMock.Object,
            _passwordHasherMock.Object,
            _jwtMock.Object,
            jwtOptions,
            _loggerMock.Object);
    }

    [Fact]
    public async Task SignupAsync_ShouldThrow_WhenEmailAlreadyRegistered()
    {
        // Arrange
        var request = new SignupRequest
        {
            Email = "test@test.com",
            Password = "password",
            CompanyName = "TestCo"
        };

        _userRepoMock
            .Setup(r => r.GetByEmailAsync(request.Email))
            .ReturnsAsync(new User());

        var service = CreateService();

        // Act
        Func<Task> act = async () => await service.SignupAsync(request);

        // Assert
        await act.Should()
                 .ThrowAsync<InvalidOperationException>()
                 .WithMessage("Email is already registered");
    }
}