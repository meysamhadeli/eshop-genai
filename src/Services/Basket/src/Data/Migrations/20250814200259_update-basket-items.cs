using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Basket.Data.Migrations
{
    /// <inheritdoc />
    public partial class updatebasketitems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_at",
                table: "basket_items");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "basket_items");

            migrationBuilder.DropColumn(
                name: "id",
                table: "basket_items");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "basket_items");

            migrationBuilder.DropColumn(
                name: "last_modified",
                table: "basket_items");

            migrationBuilder.DropColumn(
                name: "last_modified_by",
                table: "basket_items");

            migrationBuilder.DropColumn(
                name: "version",
                table: "basket_items");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "basket_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "created_by",
                table: "basket_items",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "id",
                table: "basket_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "basket_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_modified",
                table: "basket_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "last_modified_by",
                table: "basket_items",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "basket_items",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
