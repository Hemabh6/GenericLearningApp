using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenericLearningApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "menu_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Page = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Access = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    roles = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "site_settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Tagline = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequireSignIn = table.Column<bool>(type: "boolean", nullable: false),
                    AllowGuests = table.Column<bool>(type: "boolean", nullable: false),
                    AllowSignUp = table.Column<bool>(type: "boolean", nullable: false),
                    LandingMenuId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "menu_overrides",
                columns: table => new
                {
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Mode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_overrides", x => new { x.MenuItemId, x.UserId });
                    table.ForeignKey(
                        name: "FK_menu_overrides_menu_items_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "menu_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_menu_items_Position",
                table: "menu_items",
                column: "Position");

            migrationBuilder.CreateIndex(
                name: "IX_menu_overrides_UserId",
                table: "menu_overrides",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "menu_overrides");

            migrationBuilder.DropTable(
                name: "site_settings");

            migrationBuilder.DropTable(
                name: "menu_items");
        }
    }
}
