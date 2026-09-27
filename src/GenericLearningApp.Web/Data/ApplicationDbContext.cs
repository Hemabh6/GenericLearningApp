using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GenericLearningApp.Web.Data;

/// <summary>
/// Identity, plus the Data Protection keys. The keys sign the login cookie and every emailed or invite
/// link. Kept in the database (not in the container) so a redeploy doesn't sign everyone out, and so a
/// database backup restores a working site.
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
}
