using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;

namespace Enterprise.Infrastructure.Persistence.Repositories;

public class CategoryRepository(ApplicationDbContext context):GenericRepository<Category>(context),ICategoryRepository
{
    

}
