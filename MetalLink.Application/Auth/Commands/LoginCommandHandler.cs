using MediatR;
using MetalLink.Application.Interfaces;
using MetalLink.Shared.Auth;

namespace MetalLink.Application.Auth.Commands;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponseDto>
{
    private readonly IOperatorRepository _operatorRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;

    public LoginCommandHandler(
        IOperatorRepository operatorRepository,
        ITokenService tokenService,
        IPasswordHasher passwordHasher)
    {
        _operatorRepository = operatorRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var op = await _operatorRepository.GetByUsernameAsync(request.Username, cancellationToken);

        if (op == null || !op.IsActive)
            throw new UnauthorizedAccessException("Invalid username or password.");

        var expectedHash = op.PasswordHash;
        var isValid = _passwordHasher.VerifyPassword(request.Password, op.Username, expectedHash);

        if (!isValid)
            throw new UnauthorizedAccessException("Invalid username or password.");

        var token = _tokenService.GenerateToken(op);

        return new LoginResponseDto
        {
            Token = token,
            Username = op.Username,
            DisplayName = op.DisplayName,
            Role = op.Role,
            SiteId = op.SiteId
        };
    }
}
