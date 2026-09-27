using Microsoft.AspNetCore.Identity;

namespace GenericLearningApp.Web.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// The account that can never be removed, demoted or deleted from the admin area. Set only by
    /// the startup seeder from configuration; nothing in the UI can set or clear it.
    /// </summary>
    public bool IsSuperAdmin { get; set; }
}
