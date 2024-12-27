using ECommerceMVC.Data;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceMVC.ViewComponents
{
    public class MenuLoaiViewComponent : ViewComponent
    {
        readonly Hshop2023Context db;
        public MenuLoaiViewComponent(Hshop2023Context _db)
        {
            db = _db;
        }
        public IViewComponentResult Invoke()
        {
            var data = db.Loais.Select(l => new MenuLoaiVM
            {
               MaLoai = l.MaLoai , TenLoai = l.TenLoai, SoLuong = l.HangHoas.Count
            }).OrderBy(l => l.SoLuong);
            return View(data);
        }
    }
}


//ViewComponent componet chỉ có 1 action duy nhất Invoke 