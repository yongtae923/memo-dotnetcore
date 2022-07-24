using System.ComponentModel.DataAnnotations;

namespace AblyAPI.Models.Requests;

public class VerificationCodeRequestModel
{
    [Required]
    [DataType(DataType.PhoneNumber)]
    public string Phone { get; set; }
}