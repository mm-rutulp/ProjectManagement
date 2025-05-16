using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagement.Migrations
{
    /// <inheritdoc />
    public partial class ShadowResourceRestructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectAssignments_AspNetUsers_AddedByUserId",
                table: "ProjectAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ProjectAssignments_AddedByUserId",
                table: "ProjectAssignments");

            migrationBuilder.DropColumn(
                name: "AddedByUserId",
                table: "ProjectAssignments");

            migrationBuilder.DropColumn(
                name: "IsShadowResource",
                table: "ProjectAssignments");

            migrationBuilder.AddColumn<bool>(
                name: "IsShadowResourceWorklog",
                table: "Worklogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ShadowResourceId",
                table: "Worklogs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectShadowResourceAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ShadowResourceId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProjectOnBoardUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectShadowResourceAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectShadowResourceAssignments_AspNetUsers_ProjectOnBoardUserId",
                        column: x => x.ProjectOnBoardUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectShadowResourceAssignments_AspNetUsers_ShadowResourceId",
                        column: x => x.ShadowResourceId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectShadowResourceAssignments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Worklogs_ShadowResourceId",
                table: "Worklogs",
                column: "ShadowResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectShadowResourceAssignments_ProjectId",
                table: "ProjectShadowResourceAssignments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectShadowResourceAssignments_ProjectOnBoardUserId",
                table: "ProjectShadowResourceAssignments",
                column: "ProjectOnBoardUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectShadowResourceAssignments_ShadowResourceId",
                table: "ProjectShadowResourceAssignments",
                column: "ShadowResourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Worklogs_AspNetUsers_ShadowResourceId",
                table: "Worklogs",
                column: "ShadowResourceId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Worklogs_AspNetUsers_ShadowResourceId",
                table: "Worklogs");

            migrationBuilder.DropTable(
                name: "ProjectShadowResourceAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Worklogs_ShadowResourceId",
                table: "Worklogs");

            migrationBuilder.DropColumn(
                name: "IsShadowResourceWorklog",
                table: "Worklogs");

            migrationBuilder.DropColumn(
                name: "ShadowResourceId",
                table: "Worklogs");

            migrationBuilder.AddColumn<string>(
                name: "AddedByUserId",
                table: "ProjectAssignments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsShadowResource",
                table: "ProjectAssignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAssignments_AddedByUserId",
                table: "ProjectAssignments",
                column: "AddedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectAssignments_AspNetUsers_AddedByUserId",
                table: "ProjectAssignments",
                column: "AddedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
