using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoucherHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateRemainingMvpTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    details = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "campaigns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "DRAFT"),
                    max_redemptions_per_user = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaigns", x => x.id);
                    table.CheckConstraint("ck_campaigns_max_redemptions_positive", "max_redemptions_per_user > 0");
                    table.CheckConstraint("ck_campaigns_status", "status IN ('DRAFT', 'PUBLISHED', 'CANCELLED')");
                    table.CheckConstraint("ck_campaigns_valid_time_range", "ends_at > starts_at");
                    table.ForeignKey(
                        name: "FK_campaigns_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pending_registrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    verification_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pending_registrations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "voucher_offers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    points_cost = table.Column<long>(type: "bigint", nullable: false),
                    total_quantity = table.Column<int>(type: "integer", nullable: false),
                    issued_quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_voucher_offers", x => x.id);
                    table.CheckConstraint("ck_voucher_offers_issued_quantity_range", "issued_quantity >= 0 AND issued_quantity <= total_quantity");
                    table.CheckConstraint("ck_voucher_offers_points_cost_positive", "points_cost > 0");
                    table.CheckConstraint("ck_voucher_offers_total_quantity_positive", "total_quantity > 0");
                    table.ForeignKey(
                        name: "FK_voucher_offers_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_vouchers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    voucher_offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    points_spent = table.Column<long>(type: "bigint", nullable: false),
                    request_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    claimed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_vouchers", x => x.id);
                    table.CheckConstraint("ck_user_vouchers_points_spent_positive", "points_spent > 0");
                    table.ForeignKey(
                        name: "FK_user_vouchers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_vouchers_voucher_offers_voucher_offer_id",
                        column: x => x.voucher_offer_id,
                        principalTable: "voucher_offers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "point_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    balance_after = table.Column<long>(type: "bigint", nullable: false),
                    user_voucher_id = table.Column<Guid>(type: "uuid", nullable: true),
                    performed_by = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_point_transactions", x => x.id);
                    table.CheckConstraint("ck_point_transactions_amount_non_zero", "amount <> 0");
                    table.CheckConstraint("ck_point_transactions_balance_after_non_negative", "balance_after >= 0");
                    table.CheckConstraint("ck_point_transactions_type", "type IN ('ADMIN_GRANT', 'VOUCHER_REDEMPTION')");
                    table.CheckConstraint("ck_point_transactions_type_details", "(type = 'ADMIN_GRANT' AND amount > 0 AND user_voucher_id IS NULL) OR (type = 'VOUCHER_REDEMPTION' AND amount < 0 AND user_voucher_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_point_transactions_point_wallets_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "point_wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_point_transactions_user_vouchers_user_voucher_id",
                        column: x => x.user_voucher_id,
                        principalTable: "user_vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_point_transactions_users_performed_by",
                        column: x => x.performed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_actor_user_id",
                table: "audit_logs",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_created_at",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_entity_type_entity_id",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_campaigns_created_by",
                table: "campaigns",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_campaigns_status_starts_at_ends_at",
                table: "campaigns",
                columns: new[] { "status", "starts_at", "ends_at" });

            migrationBuilder.CreateIndex(
                name: "IX_pending_registrations_email",
                table: "pending_registrations",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pending_registrations_verification_token_hash",
                table: "pending_registrations",
                column: "verification_token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_point_transactions_performed_by",
                table: "point_transactions",
                column: "performed_by");

            migrationBuilder.CreateIndex(
                name: "IX_point_transactions_user_voucher_id",
                table: "point_transactions",
                column: "user_voucher_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_point_transactions_wallet_id_created_at",
                table: "point_transactions",
                columns: new[] { "wallet_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_user_vouchers_code",
                table: "user_vouchers",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_vouchers_user_id_request_key",
                table: "user_vouchers",
                columns: new[] { "user_id", "request_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_vouchers_user_id_voucher_offer_id",
                table: "user_vouchers",
                columns: new[] { "user_id", "voucher_offer_id" });

            migrationBuilder.CreateIndex(
                name: "IX_user_vouchers_voucher_offer_id",
                table: "user_vouchers",
                column: "voucher_offer_id");

            migrationBuilder.CreateIndex(
                name: "IX_voucher_offers_campaign_id",
                table: "voucher_offers",
                column: "campaign_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "pending_registrations");

            migrationBuilder.DropTable(
                name: "point_transactions");

            migrationBuilder.DropTable(
                name: "user_vouchers");

            migrationBuilder.DropTable(
                name: "voucher_offers");

            migrationBuilder.DropTable(
                name: "campaigns");
        }
    }
}
