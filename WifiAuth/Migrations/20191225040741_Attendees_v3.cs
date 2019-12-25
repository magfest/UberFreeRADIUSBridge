using Microsoft.EntityFrameworkCore.Migrations;

namespace WifiAuth.Migrations
{
    public partial class Attendees_v3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "_assignedDepartments",
                table: "Attendees",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "_badgeLabels",
                table: "Attendees",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "_assignedDepartments",
                table: "Attendees");

            migrationBuilder.DropColumn(
                name: "_badgeLabels",
                table: "Attendees");
        }
    }
}
