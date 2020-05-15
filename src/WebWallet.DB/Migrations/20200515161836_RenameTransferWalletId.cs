using Microsoft.EntityFrameworkCore.Migrations;
using WebWallet.DB.Entities;

namespace WebWallet.DB.Migrations
{
    public partial class RenameTransferWalletId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transfers_Wallets_UserWalletId",
                table: "Transfers");

            migrationBuilder.DropForeignKey(
                name: "FK_Transfers_Currencies_FromCurrencyId_UserWalletId",
                table: "Transfers");

            migrationBuilder.DropForeignKey(
                name: "FK_Transfers_Currencies_ToCurrencyId_UserWalletId",
                table: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_Transfers_UserWalletId",
                table: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_Transfers_FromCurrencyId_UserWalletId",
                table: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_Transfers_ToCurrencyId_UserWalletId",
                table: "Transfers");


            migrationBuilder.AddColumn<string>(
                name: "WalletId",
                table: "Transfers",
                nullable: true);

            migrationBuilder.Sql($"UPDATE transfers SET WalletId = UserWalletId;");

            migrationBuilder.DropColumn(
                name: "UserWalletId",
                table: "Transfers");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_WalletId",
                table: "Transfers",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_FromCurrencyId_WalletId",
                table: "Transfers",
                columns: new[] { "FromCurrencyId", "WalletId" });

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_ToCurrencyId_WalletId",
                table: "Transfers",
                columns: new[] { "ToCurrencyId", "WalletId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Transfers_Wallets_WalletId",
                table: "Transfers",
                column: "WalletId",
                principalTable: "Wallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transfers_Currencies_FromCurrencyId_WalletId",
                table: "Transfers",
                columns: new[] { "FromCurrencyId", "WalletId" },
                principalTable: "Currencies",
                principalColumns: new[] { "Currency", "WalletId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transfers_Currencies_ToCurrencyId_WalletId",
                table: "Transfers",
                columns: new[] { "ToCurrencyId", "WalletId" },
                principalTable: "Currencies",
                principalColumns: new[] { "Currency", "WalletId" },
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transfers_Wallets_WalletId",
                table: "Transfers");

            migrationBuilder.DropForeignKey(
                name: "FK_Transfers_Currencies_FromCurrencyId_WalletId",
                table: "Transfers");

            migrationBuilder.DropForeignKey(
                name: "FK_Transfers_Currencies_ToCurrencyId_WalletId",
                table: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_Transfers_WalletId",
                table: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_Transfers_FromCurrencyId_WalletId",
                table: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_Transfers_ToCurrencyId_WalletId",
                table: "Transfers");

            migrationBuilder.AddColumn<string>(
                name: "UserWalletId",
                table: "Transfers",
                type: "varchar(255) CHARACTER SET utf8mb4",
                nullable: true);

            migrationBuilder.Sql($"UPDATE transfers SET UserWalletId = WalletId;");

            migrationBuilder.DropColumn(
                name: "WalletId",
                table: "Transfers");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_UserWalletId",
                table: "Transfers",
                column: "UserWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_FromCurrencyId_UserWalletId",
                table: "Transfers",
                columns: new[] { "FromCurrencyId", "UserWalletId" });

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_ToCurrencyId_UserWalletId",
                table: "Transfers",
                columns: new[] { "ToCurrencyId", "UserWalletId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Transfers_Wallets_UserWalletId",
                table: "Transfers",
                column: "UserWalletId",
                principalTable: "Wallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transfers_Currencies_FromCurrencyId_UserWalletId",
                table: "Transfers",
                columns: new[] { "FromCurrencyId", "UserWalletId" },
                principalTable: "Currencies",
                principalColumns: new[] { "Currency", "WalletId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transfers_Currencies_ToCurrencyId_UserWalletId",
                table: "Transfers",
                columns: new[] { "ToCurrencyId", "UserWalletId" },
                principalTable: "Currencies",
                principalColumns: new[] { "Currency", "WalletId" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
