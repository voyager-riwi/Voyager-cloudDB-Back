namespace CrudCloudDb.Application.DTOs.User;

public class UserProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CurrentPlanName { get; set; } = string.Empty;
    public int DatabaseLimitPerEngine { get; set; }
    public DateTime MemberSince { get; set; }
}