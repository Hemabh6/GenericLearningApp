using GenericLearningApp.Application.Site;
using GenericLearningApp.Application.Subjects;
using GenericLearningApp.Infrastructure.Persistence;
using GenericLearningApp.Infrastructure.Site;
using GenericLearningApp.Infrastructure.Subjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GenericLearningApp.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers the learning database against PostgreSQL.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContextFactory<LearningDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<ISiteConfigService, SiteConfigService>();
        return services;
    }
}
