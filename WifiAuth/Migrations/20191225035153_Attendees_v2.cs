using Microsoft.EntityFrameworkCore.Migrations;

namespace WifiAuth.Migrations
{
    public partial class Attendees_v2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BadgeType",
                table: "Attendees",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Attendees",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDepartmentHead",
                table: "Attendees",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                table: "Attendees",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BadgeType",
                table: "Attendees");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Attendees");

            migrationBuilder.DropColumn(
                name: "IsDepartmentHead",
                table: "Attendees");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                table: "Attendees");
        }
    }
}
