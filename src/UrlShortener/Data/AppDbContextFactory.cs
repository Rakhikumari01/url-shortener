using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UrlShortener.Data
{
    /// <summary>
    /// Used only by the design-time tooling: `dotnet ef migrations add`,
    /// `dotnet ef database update` and the `dotnet ef migrations bundle` step
    /// in the Dockerfile.
    ///
    /// Without this, EF builds the application host at design time, Program.cs
    /// throws on the absent connection string, and both the local tooling and
    /// the Docker bundle stage fail. No connection is opened while scaffolding
    /// or bundling - only the model is read - so the fallback string below just
    /// needs to be syntactically valid.
    /// </summary>
    public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        private const string DesignTimeFallback ="Host=localhost;Port=5432;Database=urlshortener;Username=postgres;Password=postgres";

        public AppDbContext CreateDbContext(string[] args)
        {
            var conn = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres");
            if (string.IsNullOrWhiteSpace(conn))
            {
                conn = DesignTimeFallback;
            }

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(conn)
                .Options;

            return new AppDbContext(options);
        }
    }
}
