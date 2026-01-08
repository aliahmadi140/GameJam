// Repositories/ITeamRepository.cs
using GameJam.Models.Entities;

namespace GameJam.Repositories;

public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Team?> GetByFolderNameAsync(string folderName, CancellationToken cancellationToken = default);
    Task<bool> ExistsByFolderNameAsync(string folderName, CancellationToken cancellationToken = default);
    Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<Team> CreateAsync(Team team, CancellationToken cancellationToken = default);
    Task<List<Team>> GetAllAsync(CancellationToken cancellationToken = default);
}