using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Core.Entities;
using CrudCloudDb.Core.Enums;
using CrudCloudDb.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CrudCloudDb.Infrastructure.Repositories
{
    public class PlanRepository : IPlanRepository
    {
        private readonly ApplicationDbContext _context;

        public PlanRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Plan?> GetByPlanTypeAsync(PlanType planType)
        {
            return await _context.Plans.FirstOrDefaultAsync(p => p.PlanType == planType);
        }

        public async Task<Plan?> GetDefaultPlanAsync()
        {
            return await GetByPlanTypeAsync(PlanType.Free);
        }
    }
}