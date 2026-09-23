using JournalApp.Domain.Entities;

namespace JournalApp.Application.Auth.Dtos;

public static class UserMappings
{
    public static UserDto ToDto(this User user) =>
        new(user.Id, user.Username, user.Email, user.CreatedAt);
}
