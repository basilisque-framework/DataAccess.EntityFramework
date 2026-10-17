using Basilisque.DataAccess.EntityFramework.Base.Stamping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.DataAccess.EntityFramework.Relational.HardDelete;

/// <summary>Installs ordered stamping, soft-delete and physical deletion interceptors.</summary>
public static class HardDeleteAuditOptionsExtensions
{
    /// <summary>
    /// Installs both interceptors for an ordinary DbContext. Do not additionally call UseEFCoreStamping.
    /// Relational BaseDbContext installs these automatically.
    /// </summary>
    public static DbContextOptionsBuilder UseHardDeleteAudit(this DbContextOptionsBuilder builder, IServiceProvider scopedServices)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(scopedServices);
        return builder.UseEFCoreStamping(scopedServices)
            .AddInterceptors(scopedServices.GetRequiredService<HardDeleteAuditInterceptor>());
    }
}
