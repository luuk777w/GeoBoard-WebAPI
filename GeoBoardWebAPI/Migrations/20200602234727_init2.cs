using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GeoBoardWebAPI.Migrations
{
    public partial class init2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "BoardElements");

            migrationBuilder.AddColumn<Guid>(
                name: "ImageId",
                table: "BoardElements",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageId",
                table: "BoardElements");

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "BoardElements",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
