using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoucherHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreatePointWallets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "point_wallets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    balance = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_point_wallets", x => x.id);
                    table.CheckConstraint("ck_point_wallets_balance_non_negative", "balance >= 0");
                    table.ForeignKey(
                        name: "FK_point_wallets_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_point_wallets_user_id",
                table: "point_wallets",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "point_wallets");
        }
    }
}
