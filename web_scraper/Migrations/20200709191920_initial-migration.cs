﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace web_scraper.Migrations
{
    public partial class initialmigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobCategories",
                columns: table => new
                {
                    CategoryId = table.Column<string>(nullable: false),
                    AdvertId = table.Column<string>(nullable: true),
                    Category = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobCategories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "JobListings",
                columns: table => new
                {
                    JobListingId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdvertId = table.Column<string>(nullable: true),
                    LocationZipCode = table.Column<string>(nullable: true),
                    LocationCity = table.Column<string>(nullable: true),
                    LocationCounty = table.Column<string>(nullable: true),
                    LocationAdress = table.Column<string>(nullable: true),
                    DescriptionHtml = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    NumberOfPositions = table.Column<string>(nullable: true),
                    Admissioner = table.Column<string>(nullable: true),
                    Deadline = table.Column<string>(nullable: true),
                    website = table.Column<string>(nullable: true),
                    AdmissionerContactPerson = table.Column<string>(nullable: true),
                    AdmissionerContactPersonTelephone = table.Column<string>(nullable: true),
                    PositionType = table.Column<string>(nullable: true),
                    PositionTitle = table.Column<string>(nullable: true),
                    Industry = table.Column<string>(nullable: true),
                    section = table.Column<string>(nullable: true),
                    ForeignJobId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobListings", x => x.JobListingId);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    AdvertId = table.Column<string>(nullable: false),
                    AdvertUrl = table.Column<string>(nullable: true),
                    ImageUrl = table.Column<string>(nullable: true),
                    Position = table.Column<string>(nullable: true),
                    NumberOfPositions = table.Column<string>(nullable: true),
                    Admissioner = table.Column<string>(nullable: true),
                    ShortDescription = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.AdvertId);
                });

            migrationBuilder.CreateTable(
                name: "JobTags",
                columns: table => new
                {
                    TagId = table.Column<string>(nullable: false),
                    AdvertId = table.Column<string>(nullable: true),
                    tag = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTags", x => x.TagId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobCategories");

            migrationBuilder.DropTable(
                name: "JobListings");

            migrationBuilder.DropTable(
                name: "Jobs");

            migrationBuilder.DropTable(
                name: "JobTags");
        }
    }
}
