using Microsoft.EntityFrameworkCore.Migrations;

namespace WebWallet.DB.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Wallets",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wallets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Currency = table.Column<string>(nullable: false),
                    WalletId = table.Column<string>(nullable: false),
                    Balance = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => new { x.Currency, x.WalletId });
                    table.ForeignKey(
                        name: "FK_Currencies_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transfers",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    FromCurrencyId = table.Column<string>(nullable: true),
                    ToCurrencyId = table.Column<string>(nullable: true),
                    Amount = table.Column<double>(nullable: false),
                    ActualCurrencyRate = table.Column<double>(nullable: true),
                    UserWalletId = table.Column<string>(nullable: true),
                    State = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transfers_Wallets_UserWalletId",
                        column: x => x.UserWalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transfers_Currencies_FromCurrencyId_UserWalletId",
                        columns: x => new { x.FromCurrencyId, x.UserWalletId },
                        principalTable: "Currencies",
                        principalColumns: new[] { "Currency", "WalletId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transfers_Currencies_ToCurrencyId_UserWalletId",
                        columns: x => new { x.ToCurrencyId, x.UserWalletId },
                        principalTable: "Currencies",
                        principalColumns: new[] { "Currency", "WalletId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_WalletId",
                table: "Currencies",
                column: "WalletId");

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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transfers");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "Wallets");
        }
    }
}
