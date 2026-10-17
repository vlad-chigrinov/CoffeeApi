using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoffeeApi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCustomerName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Orders",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                // Orders placed before the field existed get a neutral name that still satisfies the 2-10 character rule.
                defaultValue: "Гость");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Orders");
        }
    }
}
