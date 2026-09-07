using Sv.Order.DTOs;

namespace Sv.Order.Repository.Interface;

public interface IAuthRepository
{
    Task<AuthUserDto?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken);
}
