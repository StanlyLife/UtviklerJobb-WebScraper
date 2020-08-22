using Microsoft.EntityFrameworkCore.Migrations;

namespace web_scraper.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Industry",
                table: "JobListings");

            migrationBuilder.DropColumn(
                name: "Modified",
                table: "JobListings");

            migrationBuilder.RenameColumn(
                name: "tag",
                table: "JobTags",
                newName: "Tag");

            migrationBuilder.AddColumn<string>(
                name: "AdmissionerDescription",
                table: "JobListings",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdvertExpires",
                table: "JobListings",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdvertModified",
                table: "JobListings",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "advertPublished",
                table: "JobListings",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JobIndustry",
                columns: table => new
                {
                    IndustryId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobId = table.Column<string>(nullable: true),
                    Industry = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobIndustry", x => x.IndustryId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobIndustry");

            migrationBuilder.DropColumn(
                name: "AdmissionerDescription",
                table: "JobListings");

            migrationBuilder.DropColumn(
                name: "AdvertExpires",
                table: "JobListings");

            migrationBuilder.DropColumn(
                name: "AdvertModified",
                table: "JobListings");

            migrationBuilder.DropColumn(
                name: "advertPublished",
                table: "JobListings");

            migrationBuilder.RenameColumn(
                name: "Tag",
                table: "JobTags",
                newName: "tag");

            migrationBuilder.AddColumn<string>(
                name: "Industry",
                table: "JobListings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Modified",
                table: "JobListings",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
