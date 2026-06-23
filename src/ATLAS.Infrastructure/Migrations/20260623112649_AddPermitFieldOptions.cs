using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATLAS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPermitFieldOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Options",
                table: "PermitField",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Options",
                table: "PermitField");
        }
    }
}
