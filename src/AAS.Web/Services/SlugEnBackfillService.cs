using AAS.Web.Data;
using AAS.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AAS.Web.Services
{
    /// <summary>
    /// Runs once at startup and ensures every public Collection has a populated
    /// SlugEn field. Uses the English CollectionTranslation when available,
    /// otherwise falls back to slugifying the original Czech Title.
    /// Subsequent runs are no-ops because they skip rows that already have a value.
    /// </summary>
    public class SlugEnBackfillService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<SlugEnBackfillService> _logger;

        public SlugEnBackfillService(IServiceProvider services, ILogger<SlugEnBackfillService> logger)
        {
            _services = services;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken ct = default)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var slug = scope.ServiceProvider.GetRequiredService<SlugService>();

            var missing = await db.Collections
                .Where(c => c.SlugEn == null || c.SlugEn == "")
                .ToListAsync(ct);

            if (missing.Count == 0)
            {
                _logger.LogInformation("SlugEn backfill: no rows to update.");
                return;
            }

            var ids = missing.Select(c => c.Id).ToList();
            var enTranslations = await db.CollectionTranslations
                .Where(t => ids.Contains(t.CollectionId) && t.LanguageCode == "en")
                .ToDictionaryAsync(t => t.CollectionId, t => t.TranslatedTitle, ct);

            // Track slugs we have already assigned in this pass to avoid clashes
            var taken = new HashSet<string>(
                await db.Collections
                    .Where(c => c.SlugEn != null)
                    .Select(c => c.SlugEn!)
                    .ToListAsync(ct),
                StringComparer.OrdinalIgnoreCase);

            int updated = 0;
            foreach (var c in missing)
            {
                string source = enTranslations.TryGetValue(c.Id, out var enTitle) && !string.IsNullOrWhiteSpace(enTitle)
                    ? enTitle
                    : c.Title;

                var baseSlug = slug.ToSlug(source);
                if (string.IsNullOrWhiteSpace(baseSlug))
                    baseSlug = $"collection-{c.Id}";

                var candidate = baseSlug;
                var n = 1;
                while (taken.Contains(candidate))
                {
                    n++;
                    candidate = $"{baseSlug}-{n}";
                }
                c.SlugEn = candidate;
                taken.Add(candidate);
                updated++;
            }

            await db.SaveChangesAsync(ct);
            _logger.LogInformation("SlugEn backfill: filled {Count} collection(s).", updated);
        }
    }
}
