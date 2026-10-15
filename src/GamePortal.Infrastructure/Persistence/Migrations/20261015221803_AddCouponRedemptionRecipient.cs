using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamePortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCouponRedemptionRecipient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CharacterId",
                table: "CouponRedemptions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CharacterName",
                table: "CouponRedemptions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServerId",
                table: "CouponRedemptions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServerName",
                table: "CouponRedemptions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CharacterId",
                table: "CouponRedemptions");

            migrationBuilder.DropColumn(
                name: "CharacterName",
                table: "CouponRedemptions");

            migrationBuilder.DropColumn(
                name: "ServerId",
                table: "CouponRedemptions");

            migrationBuilder.DropColumn(
                name: "ServerName",
                table: "CouponRedemptions");
        }
    }
}
