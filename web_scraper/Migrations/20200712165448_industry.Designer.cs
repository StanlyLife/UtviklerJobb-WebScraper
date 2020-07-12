﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using web_scraper.Data;

namespace web_scraper.Migrations
{
    [DbContext(typeof(WebScraperContext))]
    [Migration("20200712165448_industry")]
    partial class industry
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("web_scraper.models.JobCategoryModel", b =>
                {
                    b.Property<int>("CategoryId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Category")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("JobId")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("CategoryId");

                    b.ToTable("JobCategories");
                });

            modelBuilder.Entity("web_scraper.models.JobIndustryModel", b =>
                {
                    b.Property<int>("IndustryId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Industry")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("JobId")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("IndustryId");

                    b.ToTable("JobIndustry");
                });

            modelBuilder.Entity("web_scraper.models.JobModel", b =>
                {
                    b.Property<string>("JobId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Admissioner")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("AdmissionerContactPerson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("AdmissionerContactPersonTelephone")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("AdmissionerWebsite")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("AdvertUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Deadline")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DescriptionHtml")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ForeignJobId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ImageUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Industry")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LocationAdress")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LocationCity")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LocationCounty")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LocationZipCode")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Modified")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("NumberOfPositions")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OriginWebsite")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PositionHeadline")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PositionTitle")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PositionType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Sector")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ShortDescription")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("JobId");

                    b.ToTable("JobListings");
                });

            modelBuilder.Entity("web_scraper.models.JobTagsModel", b =>
                {
                    b.Property<int>("TagId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("JobId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("tag")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("TagId");

                    b.ToTable("JobTags");
                });
#pragma warning restore 612, 618
        }
    }
}
