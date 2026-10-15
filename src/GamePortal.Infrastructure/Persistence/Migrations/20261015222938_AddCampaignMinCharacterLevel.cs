using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamePortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignMinCharacterLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinCharacterLevel",
                table: "CouponCampaigns",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinCharacterLevel",
                table: "CouponCampaigns");
        }
    }
}
