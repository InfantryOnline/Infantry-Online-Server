using AccountWeb.Services;
using Database.Sqlite;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AccountWeb.Pages.Histories;

public class IndexModel : PageModel
{
    private const int PageSize = 50;

    private readonly SqliteDbContext _db;

    public IndexModel(SqliteDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public IReadOnlyList<HistoryRow> Histories { get; private set; } = [];

    public int TotalCount { get; private set; }

    public int TotalPages { get; private set; }

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (PageNumber < 1)
        {
            PageNumber = 1;
        }

        TotalCount = await _db.Histories.CountAsync(cancellationToken);
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

        if (PageNumber > TotalPages)
        {
            PageNumber = TotalPages;
        }

        Histories = await _db.Histories
            .AsNoTracking()
            .OrderByDescending(history => history.Date)
            .ThenByDescending(history => history.HistoryId)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .Select(history => new HistoryRow(
                history.HistoryId,
                history.Sender,
                history.Recipient,
                history.Zone,
                history.Arena,
                history.Command,
                history.Date))
            .ToListAsync(cancellationToken);
    }

    public sealed record HistoryRow(
        long HistoryId,
        string Sender,
        string Recipient,
        string Zone,
        string Arena,
        string Command,
        DateTime Date);
}
