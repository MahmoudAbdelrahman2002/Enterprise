using Enterprise.Domain.Entities;
using Enterprise.Domain.Specifications;

namespace Enterprise.Domain.Specifications.Services;

public sealed class ProductByIdSpecification : BaseSpecification<Product>
{
    public ProductByIdSpecification(Guid id)
        : base(product => product.Id == id)
    {
        AddInclude(product => product.Translations);
        AddInclude(product => product.Category);
    }
}
