using MediatR;
using MetalLink.Shared.Auth;

namespace MetalLink.Application.Auth.Commands;

public sealed record LoginCommand(string Username, string Password)
    : IRequest<LoginResponseDto>;
