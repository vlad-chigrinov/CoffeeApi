using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoffeeApi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTenantImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Tenants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Tenants",
                type: "TEXT",
                maxLength: 500,
                nullable: true);
        }
    }
}
