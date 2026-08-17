using Database.Sqlite;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AccountWeb.Pages.Zones;

public class IndexModel : PageModel
{
    private readonly SqliteDbContext _db;

    public IndexModel(SqliteDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<ZoneRow> Zones { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Zones = await _db.Zones
            .AsNoTracking()
            .OrderBy(zone => zone.Name)
            .Select(zone => new ZoneRow(
                zone.ZoneId,
                zone.Name,
                zone.Description,
                zone.Notice,
                zone.Password,
                zone.Active,
                zone.Ip,
                zone.Port,
                zone.OldId))
            .ToListAsync(cancellationToken);
    }

    public sealed record ZoneRow(
        long ZoneId,
        string Name,
        string Description,
        string Notice,
        string Password,
        short Active,
        string? Ip,
        int? Port,
        long OldId);
}
