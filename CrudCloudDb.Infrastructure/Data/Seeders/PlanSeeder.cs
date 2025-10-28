using Microsoft.EntityFrameworkCore;
using CrudCloudDb.Core.Entities;
using CrudCloudDb.Core.Enums;
using System;

namespace CrudCloudDb.Infrastructure.Data.Seeders
{
    public static class PlanSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Plan>().HasData(
                new Plan
                {
                    Id = Guid.Parse("b1b108e5-fcbc-4a91-8967-b545ff937016"),
                    PlanType = PlanType.Free,
                    Name = "Free Plan",
                    Price = 0.00m,
                    DatabaseLimitPerEngine = 2
                },
                new Plan
                {
                    Id = Guid.Parse("0b2a601a-1269-4818-9161-2797f54a7100"),
                    PlanType = PlanType.Intermediate,
                    Name = "Intermediate Plan",
                    Price = 5000.00m,
                    DatabaseLimitPerEngine = 5
                },
                new Plan
                {
                    Id = Guid.Parse("7be9fe44-7454-4055-8a5f-eff194532a2e"),
                    PlanType = PlanType.Advanced,
                    Name = "Advanced Plan",
                    Price = 10000.00m,
                    DatabaseLimitPerEngine = 10
                }
            );
        }
    }
}