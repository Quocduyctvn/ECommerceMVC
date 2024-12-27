using ECommerceMVC.Data;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceMVC.Controllers
{
    public class HangHoaController : Controller
    {
        private readonly Hshop2023Context context;
        public HangHoaController(Hshop2023Context _Context) {
            context = _Context;
        }
        public IActionResult Index(int? loai, string? keySearch)
        {
            var hanghoas = context.HangHoas.AsQueryable();
            if(loai.HasValue)
            {
                hanghoas = hanghoas.Where(q => q.MaLoai == loai.Value);
            }
            if (keySearch != null)
            {
                hanghoas = hanghoas.Where(q => q.TenHh.Contains(keySearch));
            }
            var result = hanghoas.Select(q => new HangHoaVM
            {
                MaHH = q.MaHh,
                TenHH = q.TenHh,
                DonGia = q.DonGia ?? 0,  // gán = 0 nếu giá trị null 
                Hinh = q.Hinh ?? "",
                MoTaNgan = q.MoTaDonVi,
                TenLoai = q.MaLoaiNavigation.TenLoai

            });
            return View(result);
        }

		public IActionResult Details(int idCategory)
		{
			if (idCategory == 0) // Đổi kiểu kiểm tra từ null thành 0 (hoặc giá trị mặc định phù hợp)
			{
				return RedirectToAction("Index", "HangHoa");
			}

			var category = context.HangHoas
				.Include(q => q.MaLoaiNavigation)
				.SingleOrDefault(q => q.MaHh == idCategory);

			if (category == null)
			{
				// Xử lý khi không tìm thấy danh mục
				// Ví dụ: có thể chuyển hướng hoặc hiển thị thông báo lỗi
				return RedirectToAction("Index", "HangHoa");
			}

			var result = new DetailHangHoaVM
			{
				MaHH = category.MaHh,
				TenHH = category.TenHh,
				DonGia = category.DonGia ?? 0,  // gán = 0 nếu giá trị null 
				ChiTiet = category.MoTa,
				Hinh = category.Hinh ?? "",
				MoTaNgan = category.MoTaDonVi,
				TenLoai = category.MaLoaiNavigation != null ? category.MaLoaiNavigation.TenLoai : string.Empty,
				SoLuongTon = "10",
				DiemDanhGia = "5",
			};

			return View(result);
		}

	}
}
