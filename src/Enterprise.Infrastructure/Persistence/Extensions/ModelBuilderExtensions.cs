using System.Linq.Expressions;
using Enterprise.Domain.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enterprise.Infrastructure.Persistence.Extensions;

public static class ModelBuilderExtensions
{
    /// <summary>Builds <c>e => !e.IsDeleted</c> via an expression tree against the entity's
    /// actual CLR type, since <c>HasQueryFilter</c> needs a filter typed to the entity, not to
    /// the <see cref="ISoftDelete"/> interface it was discovered through.</summary>
    public static void AddSoftDeleteQueryFilter(this EntityTypeBuilder entityTypeBuilder)
    {
        var clrType = entityTypeBuilder.Metadata.ClrType;
        var parameter = Expression.Parameter(clrType, "entity");
        var isDeletedProperty = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
        var notDeleted = Expression.Equal(isDeletedProperty, Expression.Constant(false));
        var lambda = Expression.Lambda(notDeleted, parameter);

        entityTypeBuilder.HasQueryFilter(lambda);
    }
}
