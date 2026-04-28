namespace InsuranceApi.Domain.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string? Email { get; set; }
    public string Phone { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public ICollection<Policy> Policies { get; set; } = new List<Policy>();
}
