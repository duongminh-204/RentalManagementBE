using System.ComponentModel.DataAnnotations;

namespace Backend.Entities;

/// <summary>
/// Tài khoản ngân hàng của Admin để nhận thanh toán đăng ký gói (VietQR).
/// </summary>
public class PlatformPaymentSetting
{
    public int Id { get; set; } = 1;

    [Required]
    [MaxLength(50)]
    public string BankName { get; set; } = string.Empty;

    /// <summary>Mã BIN ngân hàng (vd: 970436 cho VCB) hoặc logoCode VietQR (vd: VCB).</summary>
    [Required]
    [MaxLength(20)]
    public string BankId { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string AccountNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string AccountName { get; set; } = string.Empty;

    public bool IsConfigured { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
