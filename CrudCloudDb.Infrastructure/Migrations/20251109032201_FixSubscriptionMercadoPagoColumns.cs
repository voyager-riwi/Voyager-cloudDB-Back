using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrudCloudDb.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixSubscriptionMercadoPagoColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "mercado_pago_subscription_id",
                table: "subscriptions",
                newName: "mercado_pago_payment_id");

            migrationBuilder.AddColumn<string>(
                name: "mercado_pago_order_id",
                table: "subscriptions",
                type: "text",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mercado_pago_order_id",
                table: "subscriptions");

            migrationBuilder.RenameColumn(
                name: "mercado_pago_payment_id",
                table: "subscriptions",
                newName: "mercado_pago_subscription_id");

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
    }
}
