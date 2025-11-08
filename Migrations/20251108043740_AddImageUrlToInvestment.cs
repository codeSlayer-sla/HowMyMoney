using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HowsMyMoney.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrlToInvestment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Solo agregar la columna ImageUrl si no existe
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Investments",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Investments");
        }
    }
}
