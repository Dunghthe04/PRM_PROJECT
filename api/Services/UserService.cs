using Api.DTOs;
using Api.Models;
using Api.Repositories;

namespace Api.Services;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);
    Task<bool> ValidateCredentialsAsync(string username, string password);
}

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null) return null;

        return new UserDto { Id = user.Id, Username = user.Username };
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
    {
        // Simple hash logic for demo purposes (use BCrypt or similar in prod)
        var user = new User
        {
            Username = createUserDto.Username,
            PasswordHash = createUserDto.Password // Hash this!
        };

        var createdUser = await _userRepository.CreateUserAsync(user);
        return new UserDto { Id = createdUser.Id, Username = createdUser.Username };
    }

    public async Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        var user = await _userRepository.GetUserByUsernameAsync(username);
        if (user == null) return false;

        return user.PasswordHash == password; // Verify hash!
    }
}
