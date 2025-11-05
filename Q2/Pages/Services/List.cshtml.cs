using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Q2.Models;

namespace Q2.Pages.Services
{
    public class ListModel : PageModel
    {
        private readonly Prnsum25B123Context _context;
        public ListModel(Prnsum25B123Context context) => _context = context;

        // ====== FILTERS (GET) ======
        [BindProperty(SupportsGet = true)] public string? RoomTitle { get; set; }
        [BindProperty(SupportsGet = true)] public string? FeeType { get; set; }
        [BindProperty(SupportsGet = true)] public int? Month { get; set; }
        [BindProperty(SupportsGet = true)] public int? Year { get; set; }
        [BindProperty(SupportsGet = true)] public bool? Paid { get; set; }

        // ====== SORT BY YEAR (GET) ======
        [BindProperty(SupportsGet = true)] public string? Dir { get; set; } = "asc";

        // ====== EDIT  ======
        [BindProperty(SupportsGet = true)] public int? Id { get; set; }
        [BindProperty(SupportsGet = true)] public string? RoomSelected { get; set; }
        [BindProperty(SupportsGet = true)] public int? EmployeeSelected { get; set; }

        // ====== EDIT DATA ======
        public Service? Input { get; set; } = new();

        public List<Service> Results { get; set; } = new();

        // Dropdown options
        public List<SelectListItem> RoomOptions { get; set; } = new();

        public List<SelectListItem> EmployeeOptions { get; set; } = new();

        private async Task LoadOptionsAsync()
        {
            RoomOptions = await _context.Rooms
                .OrderBy(r => r.Title)
                .Select(r => new SelectListItem { Value = r.Title, Text = r.Title })
                .ToListAsync();

            EmployeeOptions = await _context.Employees
                .OrderBy(e => e.Name)
                .Select(e => new SelectListItem { Value = e.Id.ToString(), Text = e.Name })
                .ToListAsync();
        }

        private IQueryable<Service> BuildQuery()
        {
            var q = _context.Services
                .Include(s => s.RoomTitleNavigation)
                .Include(s => s.EmployeeNavigation)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(RoomTitle))
                q = q.Where(r => r.RoomTitle != null && r.RoomTitle.Contains(RoomTitle));
            if (!string.IsNullOrWhiteSpace(FeeType))
                q = q.Where(r => r.FeeType != null && r.FeeType.Contains(FeeType));
            if (Month.HasValue) q = q.Where(r => r.Month == Month.Value);
            if (Year.HasValue) q = q.Where(r => r.Year == Year.Value);
            if (Paid.HasValue) q = q.Where(r => Paid.Value ? r.PaymentDate != null : r.PaymentDate == null);

            bool asc = (Dir ?? "asc").Equals("asc", StringComparison.OrdinalIgnoreCase);
            q = asc ? q.OrderBy(x => x.Year).ThenBy(x => x.Month)
                    : q.OrderByDescending(x => x.Year).ThenByDescending(x => x.Month);

            return q;
        }

        public async Task OnGetAsync()
        {
            await LoadOptionsAsync();

            Results = await BuildQuery().ToListAsync();

            if (Id.HasValue && Id.Value > 0)
            {
                var entity = await _context.Services.AsNoTracking().FirstOrDefaultAsync(x => x.Id == Id.Value);
                if (entity != null) Input = entity;
            }
        }

        // ====== CREATE (POST) ======
        public async Task<IActionResult> OnPostCreateAsync(
            string? CreateRoomTitle,
            string? CreateFeeType,
            byte? CreateMonth,
            int? CreateYear,
            decimal? CreateAmount,
            DateOnly? CreatePaymentDate,
            int? CreateEmployee,
            string? FilterRoomTitle,
            string? FilterFeeType,
            int? FilterMonth,
            int? FilterYear,
            bool? FilterPaid,
            string? FilterDir)
        {
            // Validate FK & inputs
            if (string.IsNullOrWhiteSpace(CreateRoomTitle) ||
                !await _context.Rooms.AnyAsync(r => r.Title == CreateRoomTitle))
                ModelState.AddModelError(string.Empty, "Room không tồn tại.");

            if (CreateEmployee.HasValue &&
                !await _context.Employees.AnyAsync(e => e.Id == CreateEmployee.Value))
                ModelState.AddModelError(string.Empty, "Employee không hợp lệ.");

            if (!ModelState.IsValid)
            {
                await LoadOptionsAsync();
                // giữ filter
                RoomTitle = FilterRoomTitle; FeeType = FilterFeeType; Month = FilterMonth;
                Year = FilterYear; Paid = FilterPaid; Dir = FilterDir;
                Results = await BuildQuery().ToListAsync();
                return Page();
            }

            var entity = new Service
            {
                RoomTitle = CreateRoomTitle,
                FeeType = CreateFeeType,
                Month = CreateMonth,
                Year = CreateYear,
                Amount = CreateAmount,
                PaymentDate = CreatePaymentDate,
                Employee = CreateEmployee
            };

            _context.Services.Add(entity);
            await _context.SaveChangesAsync();

            return RedirectToPage("./List", new
            {
                RoomTitle = FilterRoomTitle,
                FeeType = FilterFeeType,
                Month = FilterMonth,
                Year = FilterYear,
                Paid = FilterPaid,
                Dir = FilterDir
            });
        }

        // ====== UPDATE (POST) ======
        public async Task<IActionResult> OnPostUpdateAsync(
            int EditId,
            string? EditRoomTitle,
            string? EditFeeType,
            byte? EditMonth,
            int? EditYear,
            decimal? EditAmount,
            DateOnly? EditPaymentDate,
            int? EditEmployee,
            string? FilterRoomTitle,
            string? FilterFeeType,
            int? FilterMonth,
            int? FilterYear,
            bool? FilterPaid,
            string? FilterDir)
        {
            var entity = await _context.Services.FirstOrDefaultAsync(x => x.Id == EditId);
            if (entity == null) return NotFound();

            if (string.IsNullOrWhiteSpace(EditRoomTitle) ||
                !await _context.Rooms.AnyAsync(r => r.Title == EditRoomTitle))
                ModelState.AddModelError(string.Empty, "Room không tồn tại.");

            if (EditEmployee.HasValue &&
                !await _context.Employees.AnyAsync(e => e.Id == EditEmployee.Value))
                ModelState.AddModelError(string.Empty, "Employee không hợp lệ.");

            if (!ModelState.IsValid)
            {
                await LoadOptionsAsync();
                RoomTitle = FilterRoomTitle; FeeType = FilterFeeType; Month = FilterMonth;
                Year = FilterYear; Paid = FilterPaid; Dir = FilterDir;
                Results = await BuildQuery().ToListAsync();
                Input = entity; // giữ lại dữ liệu edit
                return Page();
            }

            entity.RoomTitle = EditRoomTitle;
            entity.FeeType = EditFeeType;
            entity.Month = EditMonth;
            entity.Year = EditYear;
            entity.Amount = EditAmount;
            entity.PaymentDate = EditPaymentDate;
            entity.Employee = EditEmployee;

            await _context.SaveChangesAsync();

            return RedirectToPage("./List", new
            {
                RoomTitle = FilterRoomTitle,
                FeeType = FilterFeeType,
                Month = FilterMonth,
                Year = FilterYear,
                Paid = FilterPaid,
                Dir = FilterDir
            });
        }

        // ====== DELETE (POST) ======
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteAsync(
            int id,
            string? FilterRoomTitle,
            string? FilterFeeType,
            int? FilterMonth,
            int? FilterYear,
            bool? FilterPaid,
            string? FilterDir)
        {
            var entity = await _context.Services.FindAsync(id);
            if (entity == null) return NotFound();

            _context.Services.Remove(entity);
            await _context.SaveChangesAsync();

            return RedirectToPage("./List", new
            {
                RoomTitle = FilterRoomTitle,
                FeeType = FilterFeeType,
                Month = FilterMonth,
                Year = FilterYear,
                Paid = FilterPaid,
                Dir = FilterDir
            });
        }
    }
}
