using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATLAS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class A25_PermitTypeOrdering : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PermitField: int IDENTITY -> Guid (drop/recreate column)
            migrationBuilder.DropPrimaryKey(name: "PK_PermitField", table: "PermitField");
            migrationBuilder.DropColumn(name: "Id", table: "PermitField");
            migrationBuilder.AddColumn<Guid>(
                name: "Id", table: "PermitField", type: "uniqueidentifier",
                nullable: false, defaultValueSql: "NEWID()");
            migrationBuilder.AddColumn<int>(
                name: "Order", table: "PermitField", type: "int",
                nullable: false, defaultValue: 1);
            migrationBuilder.AddPrimaryKey(name: "PK_PermitField", table: "PermitField", column: "Id");
            migrationBuilder.CreateIndex(name: "IX_PermitField_Order", table: "PermitField", column: "Order");
            migrationBuilder.CreateIndex(name: "IX_PermitField_PermitTypeId", table: "PermitField", column: "PermitTypeId");
        
            // DocumentRequirement: same change
            migrationBuilder.DropPrimaryKey(name: "PK_DocumentRequirement", table: "DocumentRequirement");
            migrationBuilder.DropColumn(name: "Id", table: "DocumentRequirement");
            migrationBuilder.AddColumn<Guid>(
                name: "Id", table: "DocumentRequirement", type: "uniqueidentifier",
                nullable: false, defaultValueSql: "NEWID()");
            migrationBuilder.AddColumn<int>(
                name: "Order", table: "DocumentRequirement", type: "int",
                nullable: false, defaultValue: 1);
            migrationBuilder.AddPrimaryKey(name: "PK_DocumentRequirement", table: "DocumentRequirement", column: "Id");
            migrationBuilder.CreateIndex(name: "IX_DocumentRequirement_Order", table: "DocumentRequirement", column: "Order");
            migrationBuilder.CreateIndex(name: "IX_DocumentRequirement_PermitTypeId", table: "DocumentRequirement", column: "PermitTypeId");
        }
        
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(name: "PK_PermitField", table: "PermitField");
            migrationBuilder.DropIndex(name: "IX_PermitField_Order", table: "PermitField");
            migrationBuilder.DropIndex(name: "IX_PermitField_PermitTypeId", table: "PermitField");
            migrationBuilder.DropColumn(name: "Order", table: "PermitField");
            migrationBuilder.DropColumn(name: "Id", table: "PermitField");
            migrationBuilder.AddColumn<int>(
                name: "Id", table: "PermitField", type: "int", nullable: false)
                .Annotation("SqlServer:Identity", "1, 1");
            migrationBuilder.AddPrimaryKey(name: "PK_PermitField", table: "PermitField", columns: new[] { "PermitTypeId", "Id" });
        
            migrationBuilder.DropPrimaryKey(name: "PK_DocumentRequirement", table: "DocumentRequirement");
            migrationBuilder.DropIndex(name: "IX_DocumentRequirement_Order", table: "DocumentRequirement");
            migrationBuilder.DropIndex(name: "IX_DocumentRequirement_PermitTypeId", table: "DocumentRequirement");
            migrationBuilder.DropColumn(name: "Order", table: "DocumentRequirement");
            migrationBuilder.DropColumn(name: "Id", table: "DocumentRequirement");
            migrationBuilder.AddColumn<int>(
                name: "Id", table: "DocumentRequirement", type: "int", nullable: false)
                .Annotation("SqlServer:Identity", "1, 1");
            migrationBuilder.AddPrimaryKey(name: "PK_DocumentRequirement", table: "DocumentRequirement", columns: new[] { "PermitTypeId", "Id" });
        }
    }
}
