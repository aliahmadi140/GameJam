// Repositories/TeamRepository.cs
using GameJam.Models.Entities;
using Microsoft.EntityFrameworkCore;
using SBUGameJam.Data;

namespace GameJam.Repositories;

public class TeamRepository : ITeamRepository
{
    private readonly ApplicationDbContext _context;

    public TeamRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Team?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Teams
            .Include(t => t.Members.OrderBy(m => m.DisplayOrder))
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Team?> GetByFolderNameAsync(string folderName, CancellationToken cancellationToken = default)
    {
        return await _context.Teams
            .Include(t => t.Members.OrderBy(m => m.DisplayOrder))
            .FirstOrDefaultAsync(t => t.SanitizedFolderName == folderName, cancellationToken);
    }

    public async Task<bool> ExistsByFolderNameAsync(string folderName, CancellationToken cancellationToken = default)
    {
        return await _context.Teams
            .AnyAsync(t => t.SanitizedFolderName == folderName, cancellationToken);
    }

    public async Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        return await _context.TeamMembers
            .AnyAsync(m => m.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task<Team> CreateAsync(Team team, CancellationToken cancellationToken = default)
    {
        _context.Teams.Add(team);
        await _context.SaveChangesAsync(cancellationToken);
        return team;
    }

    public async Task<List<Team>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Teams
            .Include(t => t.Members.OrderBy(m => m.DisplayOrder))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}