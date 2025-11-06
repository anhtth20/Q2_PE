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

        // ===================== FILTERS (GET) =====================
        // Các thuộc tính này được bind từ query string (hoặc route param) khi truy cập bằng GET.
        // Dùng để lọc dữ liệu trong bảng (BuildQuery()).
        [BindProperty(SupportsGet = true)] public string? RoomTitle { get; set; }
        [BindProperty(SupportsGet = true)] public string? FeeType { get; set; }
        [BindProperty(SupportsGet = true)] public int? Month { get; set; }
        [BindProperty(SupportsGet = true)] public int? Year { get; set; }
        [BindProperty(SupportsGet = true)] public bool? Paid { get; set; }  // null = All, true = Paid, false = Unpaid

        // ===================== SORT (GET) =====================
        // Hướng sắp xếp theo NĂM (và phụ theo THÁNG). "asc" hoặc "desc".
        [BindProperty(SupportsGet = true)] public string? YearDir { get; set; } = "asc";

        // ===================== EDIT (GET) =====================
        // Nếu có Id trên URL => load bản ghi vào form Edit (Input).
        // RoomSelected / EmployeeSelected đang không dùng trong .cshtml; có thể bỏ nếu không cần.
        [BindProperty(SupportsGet = true)] public int? Id { get; set; }
        [BindProperty(SupportsGet = true)] public string? RoomSelected { get; set; }
        [BindProperty(SupportsGet = true)] public int? EmployeeSelected { get; set; }

        // Dữ liệu đổ vào form Edit (asp-for="Input.*")
        public Service? Input { get; set; } = new();

        // Kết quả hiển thị bảng
        public List<Service> Results { get; set; } = new();

        // ===================== DROPDOWN OPTIONS =====================
        // Nguồn dữ liệu cho dropdown Room / Employee ở Create & Edit.
        public List<SelectListItem> RoomOptions { get; set; } = new();
        public List<SelectListItem> EmployeeOptions { get; set; } = new();

        /// <summary>
        /// Nạp danh sách Room/Employee cho dropdown từ DB.
        /// Gọi hàm này trước khi render trang (OnGet) hoặc khi cần trả lại Page() sau validate lỗi.
        /// </summary>
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

        /// <summary>
        /// Tạo IQueryable áp dụng filter (RoomTitle/FeeType/Paid) và sort (YearDir).
        /// Thực thi bằng ToListAsync() ở nơi gọi.
        /// </summary>
        private IQueryable<Service> BuildQuery()
        {
            var q = _context.Services
                // Include để render được tên phòng / tên nhân viên trong bảng
                .Include(s => s.RoomTitleNavigation)
                .Include(s => s.EmployeeNavigation)
                .AsQueryable();

            // --------- FILTERS ----------
            if (!string.IsNullOrWhiteSpace(RoomTitle))
                q = q.Where(r => r.RoomTitle != null && r.RoomTitle.Contains(RoomTitle));
            if (!string.IsNullOrWhiteSpace(FeeType))
                q = q.Where(r => r.FeeType != null && r.FeeType.Contains(FeeType));
            if (Paid.HasValue)
                // Paid=true  => có PaymentDate
                // Paid=false => chưa có PaymentDate
                q = q.Where(r => Paid.Value ? r.PaymentDate != null : r.PaymentDate == null);

            // --------- SORT ----------
            bool asc = (YearDir ?? "asc").Equals("asc", StringComparison.OrdinalIgnoreCase);
            q = asc
                ? q.OrderBy(x => x.Year).ThenBy(x => x.Month)               // tăng dần năm, rồi tháng
                : q.OrderByDescending(x => x.Year).ThenByDescending(x => x.Month); // giảm dần năm, rồi tháng

            return q;
        }

        /// <summary>
        /// GET: nạp dropdown, build danh sách theo filter/sort, nếu có Id thì nạp dữ liệu edit.
        /// </summary>
        public async Task OnGetAsync()
        {
            await LoadOptionsAsync();
            Console.WriteLine("Id: " + Id); // debug nhẹ

            // Lấy dữ liệu cho bảng
            Results = await BuildQuery().ToListAsync();

            // Nếu có Id trên URL => nạp dữ liệu vào form Edit
            if (Id.HasValue && Id.Value > 0)
            {
                var entity = await _context.Services
                    .AsNoTracking() // chỉ hiển thị, không track để tránh conflict EF
                    .FirstOrDefaultAsync(x => x.Id == Id.Value);
                if (entity != null) Input = entity;
            }
        }

        // ===================== CREATE (POST) =====================
        /// <summary>
        /// Tạo mới bản ghi Service.
        /// - Validate khóa ngoại (RoomTitle/Employee) để tránh lỗi FK.
        /// - Nếu lỗi: nạp lại dropdown + dữ liệu bảng và trả về Page().
        /// - Nếu OK: lưu và RedirectToPage (PRG) đồng thời giữ filter (preserve).
        /// </summary>
        public async Task<IActionResult> OnPostCreateAsync(
            string? CreateRoomTitle,
            string? CreateFeeType,
            byte? CreateMonth,
            int? CreateYear,
            decimal? CreateAmount,
            DateOnly? CreatePaymentDate,
            int? CreateEmployee,
            // các filter gửi kèm theo form để preserve sau khi POST
            string? FilterRoomTitle,
            string? FilterFeeType,
            int? FilterMonth,
            int? FilterYear,
            bool? FilterPaid,
            string? FilterDir)
        {
            // ----- VALIDATE FK -----
            if (string.IsNullOrWhiteSpace(CreateRoomTitle) ||
                !await _context.Rooms.AnyAsync(r => r.Title == CreateRoomTitle))
                ModelState.AddModelError(string.Empty, "Room không tồn tại.");

            if (CreateEmployee.HasValue &&
                !await _context.Employees.AnyAsync(e => e.Id == CreateEmployee.Value))
                ModelState.AddModelError(string.Empty, "Employee không hợp lệ.");

            // Nếu có lỗi => trả về Page() với dropdown & danh sách đã build theo filter
            if (!ModelState.IsValid)
            {
                await LoadOptionsAsync();

                // Gán lại filter để BuildQuery() lấy đúng
                RoomTitle = FilterRoomTitle;
                FeeType = FilterFeeType;
                Month = FilterMonth;
                Year = FilterYear;
                Paid = FilterPaid;
                YearDir = FilterDir;

                Results = await BuildQuery().ToListAsync();
                return Page(); // hiển thị lỗi ModelState trong view
            }

            // ----- TẠO ENTITY -----
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

            // ----- PRG: Redirect kèm filter để "lộ" lại filter trên URL -----
            return RedirectToPage("./List", new
            {
                RoomTitle = FilterRoomTitle,
                FeeType = FilterFeeType,
                Month = FilterMonth,
                Year = FilterYear,
                Paid = FilterPaid,
                YearDir = FilterDir   // dùng đúng key "YearDir" để bind vào property
            });
        }

        // ===================== UPDATE (POST) =====================
        /// <summary>
        /// Cập nhật bản ghi Service theo EditId.
        /// - Validate FK
        /// - Nếu lỗi: trả về Page() giữ Input + danh sách
        /// - Nếu OK: lưu và PRG (giữ filter)
        /// </summary>
        public async Task<IActionResult> OnPostUpdateAsync(
            int EditId,
            string? EditRoomTitle,
            string? EditFeeType,
            byte? EditMonth,
            int? EditYear,
            decimal? EditAmount,
            DateOnly? EditPaymentDate,
            int? EditEmployee,
            // preserve filters
            string? FilterRoomTitle,
            string? FilterFeeType,
            int? FilterMonth,
            int? FilterYear,
            bool? FilterPaid,
            string? FilterDir)
        {
            // Tìm entity cần sửa
            var entity = await _context.Services.FirstOrDefaultAsync(x => x.Id == EditId);
            if (entity == null) return NotFound();

            // ----- VALIDATE FK -----
            if (string.IsNullOrWhiteSpace(EditRoomTitle) ||
                !await _context.Rooms.AnyAsync(r => r.Title == EditRoomTitle))
                ModelState.AddModelError(string.Empty, "Room không tồn tại.");

            if (EditEmployee.HasValue &&
                !await _context.Employees.AnyAsync(e => e.Id == EditEmployee.Value))
                ModelState.AddModelError(string.Empty, "Employee không hợp lệ.");

            // Trường hợp lỗi: nạp lại dropdown + danh sách + giữ Input để asp-for hiển thị
            if (!ModelState.IsValid)
            {
                await LoadOptionsAsync();

                RoomTitle = FilterRoomTitle;
                FeeType = FilterFeeType;
                Month = FilterMonth;
                Year = FilterYear;
                Paid = FilterPaid;
                YearDir = FilterDir;

                Results = await BuildQuery().ToListAsync();
                Input = entity; // đẩy lại dữ liệu lên form Edit
                return Page();
            }

            // ----- ÁP DỮ LIỆU CẬP NHẬT -----
            entity.RoomTitle = EditRoomTitle;
            entity.FeeType = EditFeeType;
            entity.Month = EditMonth;
            entity.Year = EditYear;
            entity.Amount = EditAmount;
            entity.PaymentDate = EditPaymentDate;
            entity.Employee = EditEmployee;

            await _context.SaveChangesAsync();

            // ----- PRG: Redirect giữ filter -----
            return RedirectToPage("./List", new
            {
                RoomTitle = FilterRoomTitle,
                FeeType = FilterFeeType,
                Month = FilterMonth,
                Year = FilterYear,
                Paid = FilterPaid,
                YearDir = FilterDir
            });
        }

        // ===================== DELETE (POST) =====================
        /// <summary>
        /// Xóa bản ghi theo id, sau đó PRG (giữ filter).
        /// </summary>
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteAsync(
            int id,
            // preserve filters
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

            // PRG: redirect về trang list kèm filter như trước khi xóa
            return RedirectToPage("./List", new
            {
                RoomTitle = FilterRoomTitle,
                FeeType = FilterFeeType,
                Month = FilterMonth,
                Year = FilterYear,
                Paid = FilterPaid,
                YearDir = FilterDir
            });
        }
    }
}
