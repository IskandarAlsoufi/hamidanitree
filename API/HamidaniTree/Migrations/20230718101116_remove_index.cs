using Microsoft.EntityFrameworkCore.Migrations;

namespace HamidaniTree.Migrations
{
    public partial class remove_index : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_People_Password",
                table: "People");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_People_Password",
                table: "People",
                column: "Password",
                unique: true);
        }
    }
}
