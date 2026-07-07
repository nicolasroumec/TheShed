using Microsoft.EntityFrameworkCore;
using TheShed.Server.Data;
using TheShed.Server.Enums;
using TheShed.Shared.Models.DTOs.Tags;
using TheShed.Shared.Models.Entities;

namespace TheShed.Server.Services
{
    public class TagService : ITagService
    {
        private readonly TheShedContext _db;

        public TagService(TheShedContext db) => _db = db;

        public async Task<EntryResult<IReadOnlyList<TagResponse>>> ListAsync(int userId, CancellationToken ct = default)
        {
            var tags = await _db.Tags
                .Where(t => t.UserId == userId)
                .OrderBy(t => t.Name)
                .Select(t => new TagResponse { Id = t.Id, Name = t.Name })
                .ToListAsync(ct);

            return EntryResult<IReadOnlyList<TagResponse>>.Ok(tags);
        }

        public async Task<EntryResult<TagResponse>> CreateAsync(int userId, TagCreateRequest request, CancellationToken ct = default)
        {
            var name = request.Name.Trim();

            // Reject a name the user already uses; tags are meant to be a small, unique set.
            var exists = await _db.Tags.AnyAsync(t => t.UserId == userId && t.Name == name, ct);
            if (exists)
            {
                return EntryResult<TagResponse>.Fail(EntryError.Conflict);
            }

            var tag = new Tag { UserId = userId, Name = name };
            _db.Tags.Add(tag);
            await _db.SaveChangesAsync(ct);

            return EntryResult<TagResponse>.Ok(new TagResponse { Id = tag.Id, Name = tag.Name });
        }

        public async Task<EntryResult<TagResponse>> UpdateAsync(int userId, int tagId, TagUpdateRequest request, CancellationToken ct = default)
        {
            var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == tagId && t.UserId == userId, ct);
            if (tag is null)
            {
                return EntryResult<TagResponse>.Fail(EntryError.NotFound);
            }

            var name = request.Name.Trim();
            var clash = await _db.Tags.AnyAsync(t => t.UserId == userId && t.Name == name && t.Id != tagId, ct);
            if (clash)
            {
                return EntryResult<TagResponse>.Fail(EntryError.Conflict);
            }

            tag.Name = name;
            await _db.SaveChangesAsync(ct);

            return EntryResult<TagResponse>.Ok(new TagResponse { Id = tag.Id, Name = tag.Name });
        }

        public async Task<EntryResult<bool>> DeleteAsync(int userId, int tagId, CancellationToken ct = default)
        {
            var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == tagId && t.UserId == userId, ct);
            if (tag is null)
            {
                return EntryResult<bool>.Fail(EntryError.NotFound);
            }

            // ponytail: soft-deleting the tag hides it via the global query filter, so the
            // orphaned PasswordEntryTag rows stop resolving. Purge the join rows if they ever
            // pile up enough to matter.
            _db.Tags.Remove(tag);
            await _db.SaveChangesAsync(ct);

            return EntryResult<bool>.Ok(true);
        }
    }
}
