using AutoMapper;
using ECommerceMVC.Data;
using ECommerceMVC.Helpers;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerceMVC.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly Hshop2023Context context;
        private readonly IMapper mapper;

        public KhachHangController(Hshop2023Context _context, IMapper _mapper)
        {
            context = _context;
            mapper = _mapper;
        }
        #region Register account User 
        public IActionResult DangKy()
        {
            return View();
        }
        [HttpPost]
		public IActionResult DangKy(RegisterVM  model, IFormFile Hinh)
		{
            if (ModelState.IsValid)
            {
                try
                {
                    var khachHang = mapper.Map<KhachHang>(model);
                    khachHang.RandomKey = MyUtil.GenerateRamdomKey();    // gọi  phương thức GenerateRamdomKey trong class MyUtil để random 
                    khachHang.MatKhau = model.MatKhau.ToMd5Hash(khachHang.RandomKey);
                    khachHang.HieuLuc = true;//sẽ xử lý khi dùng Mail để active
                    khachHang.VaiTro = 0;

                    if (Hinh != null)
                    {
                        khachHang.Hinh = MyUtil.UploadHinh(Hinh, "KhachHang");
                    }

                    context.Add(khachHang);
                    context.SaveChanges();
                    return RedirectToAction("Index", "HangHoa");
                }
                catch (Exception ex)
                {
                    var mess = $"{ex.Message} shh";
                }
            }
            return View();
        }
        #endregion

        #region LogIn account User
        [HttpGet]
        public IActionResult DangNhap(string? ReturnUrl) // nhận đối số ReturnUrl từ trình duyệt để rtraar về trang mà user y/c 
        {
            ViewBag.ReturnUrl = ReturnUrl;   // truyền viewbag của returnurl sang view => view chuyển ReturnUrl làm đối số của  action "Post" 
            return View();
        }
        public async Task<IActionResult> DangNhap(LoginVM model, string? returnurl) // nhận đối số ReturnUrl từ view truyền lên  
        {
            ViewBag.ReturnUrl = returnurl;
            if(ModelState.IsValid)
            {
                var khachhang = context.KhachHangs.SingleOrDefault(
                    kh => kh.MaKh == model.UserName
                );
                if(khachhang == null)
                {
                    ModelState.AddModelError("Loi","Tai Khoan khong ton tai");
                }
                else
                {
                    if (!khachhang.HieuLuc)
                    {
						ModelState.AddModelError("Loi", "Tai Khoan da bi Khoa - vui long lien he Admin");
					}
                    else
                    {
                        if(khachhang.MatKhau == model.Password.ToMd5Hash(khachhang.RandomKey))
                        {
							ModelState.AddModelError("Loi", "Sai thong tin Dang Nhap");
						}
                        else
                        {
                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Email, khachhang.Email),
                                new Claim(ClaimTypes.Name, khachhang.HoTen),
                                new Claim(MySetting.CLAIM_CUSTOMERID, khachhang.MaKh),

                                //claim - role động 
                                new Claim(ClaimTypes.Role ,"Customer")
                            };
                            var claimsIdentity = new ClaimsIdentity(claims,
                                CookieAuthenticationDefaults.AuthenticationScheme);
                            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                            await HttpContext.SignInAsync(claimsPrincipal);

                            if (Url.IsLocalUrl(returnurl))
                            {
                                return Redirect(returnurl);
                            }
                            else
                            {
                                return Redirect("/");
                            }
                        }
					}
                }
            }
            return View();
        }
        #endregion 


        [Authorize]
        public IActionResult Profile()
        {
            return View();
        }
        
          [Authorize]
        public async Task<IActionResult> DangXuat()
        {
            await HttpContext.SignOutAsync();
            return Redirect("/");
        }
    }
}
