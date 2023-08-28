using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

namespace HamidaniTree.Migrations
{
    public partial class initialize : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    BirthDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    DeathDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsDead = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Gender = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Photo = table.Column<byte[]>(type: "varbinary(6000)", maxLength: 6000, nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    FacebookUri = table.Column<string>(type: "text", nullable: true),
                    LastLogin = table.Column<DateTime>(type: "datetime", nullable: true),
                    ParentID = table.Column<int>(type: "int", nullable: true),
                    Password = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    ParentLeft = table.Column<int>(type: "int", nullable: false),
                    ParentRight = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.ID);
                    table.ForeignKey(
                        name: "FK_People_People_ParentID",
                        column: x => x.ParentID,
                        principalTable: "People",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Authontications",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    PersonID = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true),
                    ExpireDate = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authontications", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Authontications_People_PersonID",
                        column: x => x.PersonID,
                        principalTable: "People",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Authontications_PersonID",
                table: "Authontications",
                column: "PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_Authontications_Token",
                table: "Authontications",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_People_ParentID",
                table: "People",
                column: "ParentID");

            migrationBuilder.CreateIndex(
                name: "IX_People_Password",
                table: "People",
                column: "Password",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Authontications");

            migrationBuilder.DropTable(
                name: "People");
        }
    }
}
