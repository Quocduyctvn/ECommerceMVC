using System.ComponentModel.DataAnnotations;

namespace ECommerceMVC.ViewModels
{
    public class LoginVM
    {
        [Display(Name = "Tên Đăng Nhập")]
        [Required(ErrorMessage= "*")]
        [MaxLength(20, ErrorMessage ="^Tối đa 20 ký tự ")]
        public string UserName { get; set; }
        [Display(Name = "Mật Khẩu ")]
        [Required(ErrorMessage = "*")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
