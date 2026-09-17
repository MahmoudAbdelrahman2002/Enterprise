using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Enums;
using Enterprise.Infrastructure.Common;
using Enterprise.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Enterprise.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=EnterpriseDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");

        var noOpCurrentUser = new DesignTimeCurrentUserService();
        var interceptor = new AuditableEntitySaveChangesInterceptor(noOpCurrentUser, new SystemDateTime());

        return new ApplicationDbContext(optionsBuilder.Options, interceptor);
    }

    private sealed class DesignTimeCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => null;
        public string? Email => "design-time";
        public bool IsAuthenticated => false;
        public UserType? UserType => null;
        public Guid? ProviderId => null;
        public IReadOnlyCollection<string> Roles => [];
        public IReadOnlyCollection<string> Permissions => [];
    }
}
