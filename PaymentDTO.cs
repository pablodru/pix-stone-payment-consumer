using System.ComponentModel.DataAnnotations;
using System.Text.Json;

public class PaymentMessage
{
    public CreatePaymentDTO DTO { get; set; }
    public CreatePaymentResponse Response { get; set; }

    public string SerializeToJson()
    {
        return JsonSerializer.Serialize(this);
    }

    public static PaymentMessage DeserializeFromJson(string json)
    {
        return JsonSerializer.Deserialize<PaymentMessage>(json);
    }
}

public class CreatePaymentDTO
{

    [Required(ErrorMessage = "The origin is required.")]
    public required OriginDTO Origin { get; set; }
    [Required(ErrorMessage = "The destiny is required.")]
    public required DestinyDTO Destiny { get; set; }
    [Required(ErrorMessage = "The amount is required.")]
    public required int Amount { get; set; }
    public string? Description { get; set; }
}

public class OriginDTO
{
    public required UserDataDTO User { get; set; }
    public required AccountDTO Account { get; set; }
}

public class DestinyDTO
{
    public required KeyInfosDTO Key { get; set; }
}

public class KeyInfosDTO
{
    [Required(ErrorMessage = "The key value is required.")]
    public string Value { get; set; }

    [Required(ErrorMessage = "The key type is required.")]
    [RegularExpression("^(CPF|Email|Phone|Random)$", ErrorMessage = "The key type must be CPF, Email, Phone, or Random.")]
    public string Type { get; set; }
}

public class UserDataDTO
{
    [Required(ErrorMessage = "The CPF is required.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "The CPF must have 11 numbers.")]
    public string Cpf { get; set; }
}

public class AccountDTO
{
    [Required(ErrorMessage = "The Account number is required.")]
    [RegularExpression("^[0-9]{9}$", ErrorMessage = "The Account number must contain exactly 9 numbers.")]
    public string Number { get; set; }

    [Required(ErrorMessage = "The Agency is required.")]
    [RegularExpression("^[0-9]{4}$", ErrorMessage = "The Agency must contain exactly 4 numbers.")]
    public string Agency { get; set; }
}

public class CreatePaymentResponse
{
    public int Id { get; set; }
}