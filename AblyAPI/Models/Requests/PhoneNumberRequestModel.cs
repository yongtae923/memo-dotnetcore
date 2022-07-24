using System.ComponentModel.DataAnnotations;

namespace AblyAPI.Models.Requests;

public class PhoneNumberRequestModel
{
    [Required]
    [DataType(DataType.PhoneNumber)]
    public string Phone { get; set; }
}