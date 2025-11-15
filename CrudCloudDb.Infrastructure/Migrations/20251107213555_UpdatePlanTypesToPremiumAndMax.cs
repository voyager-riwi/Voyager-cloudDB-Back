using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrudCloudDb.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePlanTypesToPremiumAndMax : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "plans",
                keyColumn: "id",
                keyValue: new Guid("0b2a601a-1269-4818-9161-2797f54a7100"),
                column: "name",
                value: "Premium Plan");

            migrationBuilder.UpdateData(
                table: "plans",
                keyColumn: "id",
                keyValue: new Guid("7be9fe44-7454-4055-8a5f-eff194532a2e"),
                column: "name",
                value: "Max Plan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "plans",
                keyColumn: "id",
                keyValue: new Guid("0b2a601a-1269-4818-9161-2797f54a7100"),
                column: "name",
                value: "Intermediate Plan");

            migrationBuilder.UpdateData(
                table: "plans",
                keyColumn: "id",
                keyValue: new Guid("7be9fe44-7454-4055-8a5f-eff194532a2e"),
                column: "name",
                value: "Advanced Plan");
        }
    }
}
