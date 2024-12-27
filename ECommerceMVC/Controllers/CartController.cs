using ECommerceMVC.Data;
using ECommerceMVC.Helpers;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceMVC.Controllers
{
    public class CartController : Controller
    {
		private readonly Hshop2023Context context;


		// thêm paypal vào constructor 
		private readonly PaypalClient _paypalClient;
		public CartController(Hshop2023Context _Context, PaypalClient paypalClient)
		{
			context = _Context;
			_paypalClient = paypalClient;
		}

											// lấy dữ liệu từ section của biến CART_KEY có kiểu dữ liệu List<CartItem> 
											// sd biểu thức lamda sau dấu "=>" để gán giá trị cho thuộc tính Cart
		public List<CartItem> Cart => HttpContext.Session.Get<List<CartItem>>(MySetting.CART_KEY) ?? new List<CartItem>();

		public IActionResult Index()
		{
			return View(Cart);   // Gửi Data trong section sang View 
		}
		public IActionResult AddToCart(int id, int quantity = 1)
		{
			var gioHang = Cart;
			var item = gioHang.SingleOrDefault(p => p.MaHH == id);   // kiểm tra và trả  về SP nếu sp có  trong section 
			if (item == null) // nếu sp chưa có trong trong section, tiến hành tạo mới 
			{
				var hangHoa = context.HangHoas.SingleOrDefault(p => p.MaHh == id);
				if (hangHoa == null)
				{
					TempData["Message"] = $"Không tìm thấy hàng hóa có mã {id}";
					return Redirect("/404");
				}
				item = new CartItem
				{
					MaHH = hangHoa.MaHh,
					TenHH = hangHoa.TenHh,
					DonGia = hangHoa.DonGia ?? 0,
					Hinh = hangHoa.Hinh ?? string.Empty,
					SoLuong = quantity
				};
				gioHang.Add(item); // thêm 1 đối tượng vào đối tượng cha 
			}
			else
			{
				item.SoLuong += quantity;  // cập nhâpk lại số lượng khi đơn hàng có trong section 
			}

			HttpContext.Session.Set(MySetting.CART_KEY, gioHang);  //	CẬP NHẬT LẠI SECTION 

			return RedirectToAction("Index");
		}
		#region RemoveCart 
		public IActionResult RemoveCart(int id)
        {
            var gioHang = Cart;
            var item = gioHang.SingleOrDefault(p => p.MaHH == id);   // tieemf kism laij trong section
            if (item != null)
            {
                gioHang.Remove(item);
                HttpContext.Session.Set(MySetting.CART_KEY, gioHang);

            }
            return RedirectToAction("Index");
        }
		#endregion

		[Authorize]
		public IActionResult PaymentSuccess()
		{
			return View("Success");
		}

		#region Checkout
		public async Task<IActionResult> Checkout()
		{
			// kiểm tra lần cuối, chắc chắn đã có sp trong giỏ hàng
			if(Cart.Count == 0)
			{
				return Redirect("/");
			}

			//truyền ViewBag qua View 
			ViewBag.PaypalClientId = _paypalClient.ClientId;
			return View(Cart);
		}

        [Authorize]
		[HttpPost]
        public async Task<IActionResult> Checkout(CheckoutVM model)
        {
            // kiểm tra lần cuối, chắc chắn đã có sp trong giỏ hàng 
            if (ModelState.IsValid)
            {
                // lấy id ng dùng từ claims, vì trc khi thanh toán pãi đang nhập  TT 
                var customerId = HttpContext.User.Claims.SingleOrDefault(p => p.Type == MySetting.CLAIM_CUSTOMERID).Value;
                // TH1: ng dùng lấy thông tin từ claims
                // TH2:  ng dùng nhập ms thông tin 

                //=>TH1:
                var khachhang = new KhachHang();

                if (customerId != null)
                {
                    if (model.GiongKhachHang)
                    {
                        khachhang = context.KhachHangs.SingleOrDefault(kh => kh.MaKh == customerId);
                    }
					var hoadon = new HoaDon
					{
						MaKh = customerId,
						HoTen = model.HoTen ?? khachhang.HoTen,
						DiaChi = model.DiaChi ?? khachhang.DiaChi,
						SoDienThoai = model.DienThoai ?? khachhang.DienThoai,
						NgayDat = DateTime.Now,
						CachThanhToan = "COD",
						CachVanChuyen = "GRAB",
						MaTrangThai = 0,
						GhiChu = model.GhiChu
					};

					// thực hiên mở kết nối csdl an toàn 
                    context.Database.BeginTransaction();
                    try 
					{
						// thực hiện Commit An toàn 
						context.Database.CommitTransaction();
						context.Add(hoadon);
						context.SaveChanges();

						// tạo 1 OPP chứa 12 danh sách các CTHD 
						var cthd = new List<ChiTietHd>();  // do thêm 1 nhóm các opp cthd vào bảng HOADON nên dùng LIst 
						foreach(var item in Cart)
						{
							// thêm CTHD vào bảng 
							cthd.Add(new ChiTietHd
							{
								MaHd = hoadon.MaHd,
								SoLuong = item.SoLuong,
								DonGia = item.DonGia,
								MaHh = item.MaHH,
								GiamGia = 0
							});
						}
						// Add danh sách các CTHD => dungf từ khóa AddRange(...)
						context.AddRange(cthd);
						context.SaveChanges();
						// sau khi lưu CTDH, tức đặt hàng thành công thì tiến hành SET lại sesion là 1 OPP rỗng 
						HttpContext.Session.Set<List<CartItem>>(MySetting.CART_KEY, new List<CartItem>());
						return View("Success");
					}
					catch
					{
						context.Database.RollbackTransaction();
					}
                }

            }
            return View(Cart);
        }
		#endregion
		#region Paypal Payment 
		[Authorize]
		[HttpPost("/Cart/create-paypal-order")]
		public async Task<IActionResult> CreatePaypalOrder(CancellationToken cancellationToken)
		{
			//  thông tin đơn hàng gửi qua paypal
			var tongTien = Cart.Sum(p => p.ThanhTien).ToString();
			var donViTienTe = "USD";
			var maDonHangThamChieu = "DH" + DateTime.Now.Ticks.ToString();

			try
			{
				var response = await _paypalClient.CreateOrder(tongTien, donViTienTe, maDonHangThamChieu);
				return Ok(response);
			}
			catch(Exception ex)
			{
				var error = ex.Message;
				return BadRequest(error);
			}
		}

		[Authorize]
		[HttpPost("/Cart/capture-paypal-order")]
		public async Task<IActionResult> CapturePaypalOrder(string orderId, CancellationToken cancellationToken)
		{
			try
			{
				var response = await _paypalClient.CaptureOrder(orderId);

				// Them methob luu vao database 
				return Ok(response);
			}
			catch (Exception ex)
			{
				var error = ex.Message;
				return BadRequest(error);
			}
		}
		#endregion
	}
}
