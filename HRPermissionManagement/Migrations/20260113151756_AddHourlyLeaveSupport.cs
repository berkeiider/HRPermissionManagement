using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRPermissionManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddHourlyLeaveSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHourly",
                table: "LeaveTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<double>(
                name: "NumberOfDays",
                table: "LeaveRequests",
                type: "float",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EndHour",
                table: "LeaveRequests",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "StartHour",
                table: "LeaveRequests",
                type: "time",
                nullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "AnnualLeaveRight",
                table: "Employees",
                type: "float",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.UpdateData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 1,
                column: "AnnualLeaveRight",
                value: 30.0);

            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsHourly",
                value: false);

            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "IsHourly",
                value: false);

            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "IsHourly",
                value: false);

            migrationBuilder.InsertData(
                table: "LeaveTypes",
                columns: new[] { "Id", "DoesItAffectBalance", "IsHourly", "Name" },
                values: new object[] { 4, true, true, "Saatlik İzin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DropColumn(
                name: "IsHourly",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "EndHour",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "StartHour",
                table: "LeaveRequests");

            migrationBuilder.AlterColumn<int>(
                name: "NumberOfDays",
                table: "LeaveRequests",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<int>(
                name: "AnnualLeaveRight",
                table: "Employees",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.UpdateData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 1,
                column: "AnnualLeaveRight",
                value: 30);
        }
    }
}
