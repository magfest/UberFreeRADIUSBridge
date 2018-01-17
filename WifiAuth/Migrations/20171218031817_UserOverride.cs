using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace WifiAuth.Migrations
{
    public partial class UserOverride : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserOverrides",
                columns: table => new
                {
                    Login = table.Column<string>(nullable: false),
                    Override = table.Column<int>(nullable: false),
                    PasswordOverride = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOverrides", x => x.Login);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserOverrides");
        }
    }
}
