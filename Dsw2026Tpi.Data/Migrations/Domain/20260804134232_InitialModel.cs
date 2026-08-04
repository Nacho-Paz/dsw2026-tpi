using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsw2026Tpi.Data.Migrations.Domain
{
    /// <inheritdoc />
    public partial class InitialModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AvailabilitySlots_AVAILABILITYRULES_AvailabilityRuleId1",
                table: "AvailabilitySlots");

            migrationBuilder.DropIndex(
                name: "IX_AvailabilitySlots_AvailabilityRuleId",
                table: "AvailabilitySlots");

            migrationBuilder.DropIndex(
                name: "IX_AvailabilitySlots_AvailabilityRuleId1",
                table: "AvailabilitySlots");

            migrationBuilder.DropColumn(
                name: "AvailabilityRuleId1",
                table: "AvailabilitySlots");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "AvailabilitySlots",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "AvailabilitySlots",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)");

            migrationBuilder.AlterColumn<bool>(
                name: "Deleted",
                table: "AvailabilitySlots",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilitySlots_AvailabilityRuleId_SlotDate_StartTime",
                table: "AvailabilitySlots",
                columns: new[] { "AvailabilityRuleId", "SlotDate", "StartTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AvailabilitySlots_AvailabilityRuleId_SlotDate_StartTime",
                table: "AvailabilitySlots");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "AvailabilitySlots",
                type: "int",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "AvailabilitySlots",
                type: "varbinary(max)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<bool>(
                name: "Deleted",
                table: "AvailabilitySlots",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "AvailabilityRuleId1",
                table: "AvailabilitySlots",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilitySlots_AvailabilityRuleId",
                table: "AvailabilitySlots",
                column: "AvailabilityRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilitySlots_AvailabilityRuleId1",
                table: "AvailabilitySlots",
                column: "AvailabilityRuleId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AvailabilitySlots_AVAILABILITYRULES_AvailabilityRuleId1",
                table: "AvailabilitySlots",
                column: "AvailabilityRuleId1",
                principalTable: "AVAILABILITYRULES",
                principalColumn: "Id");
        }
    }
}
