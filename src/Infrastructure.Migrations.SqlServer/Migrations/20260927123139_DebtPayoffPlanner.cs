using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budge.Infrastructure.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class DebtPayoffPlanner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyAdminFee",
                table: "CreditFacilities",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "CreditFacilities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE [CreditFacilities] SET [Type] = CASE WHEN [Kind] = 1 THEN 1 ELSE 0 END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthlyAdminFee",
                table: "CreditFacilities");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "CreditFacilities");
        }
    }
}
