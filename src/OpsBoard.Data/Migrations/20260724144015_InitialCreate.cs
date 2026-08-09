using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpsBoard.Data.Migrations;

/// <inheritdoc />
public partial class _20260724144015_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DatasetMetadata",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false),
                Profile = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Seed = table.Column<int>(type: "integer", nullable: false),
                ReferenceTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ModelVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DatasetMetadata", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "SlaPolicies",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SlaPolicies", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Tags",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tags", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Teams",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Teams", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                DisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "SlaTargets",
            columns: table => new
            {
                SlaPolicyId = table.Column<long>(type: "bigint", nullable: false),
                Severity = table.Column<int>(type: "integer", nullable: false),
                ResolutionMinutes = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SlaTargets", x => new { x.SlaPolicyId, x.Severity });
                table.ForeignKey(
                    name: "FK_SlaTargets_SlaPolicies_SlaPolicyId",
                    column: x => x.SlaPolicyId,
                    principalTable: "SlaPolicies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Services",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                TeamId = table.Column<long>(type: "bigint", nullable: false),
                SlaPolicyId = table.Column<long>(type: "bigint", nullable: false),
                Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Services", x => x.Id);
                table.ForeignKey(
                    name: "FK_Services_SlaPolicies_SlaPolicyId",
                    column: x => x.SlaPolicyId,
                    principalTable: "SlaPolicies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Services_Teams_TeamId",
                    column: x => x.TeamId,
                    principalTable: "Teams",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Incidents",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ServiceId = table.Column<long>(type: "bigint", nullable: false),
                AssigneeId = table.Column<long>(type: "bigint", nullable: true),
                Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                Severity = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                AcknowledgedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                LastActivityAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                SlaDueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Incidents", x => x.Id);
                table.ForeignKey(
                    name: "FK_Incidents_Services_ServiceId",
                    column: x => x.ServiceId,
                    principalTable: "Services",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Incidents_Users_AssigneeId",
                    column: x => x.AssigneeId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "IncidentPauses",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                IncidentId = table.Column<long>(type: "bigint", nullable: false),
                Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IncidentPauses", x => x.Id);
                table.ForeignKey(
                    name: "FK_IncidentPauses_Incidents_IncidentId",
                    column: x => x.IncidentId,
                    principalTable: "Incidents",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "IncidentStatusChanges",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                IncidentId = table.Column<long>(type: "bigint", nullable: false),
                ChangedById = table.Column<long>(type: "bigint", nullable: false),
                FromStatus = table.Column<int>(type: "integer", nullable: true),
                ToStatus = table.Column<int>(type: "integer", nullable: false),
                ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IncidentStatusChanges", x => x.Id);
                table.ForeignKey(
                    name: "FK_IncidentStatusChanges_Incidents_IncidentId",
                    column: x => x.IncidentId,
                    principalTable: "Incidents",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_IncidentStatusChanges_Users_ChangedById",
                    column: x => x.ChangedById,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "IncidentTags",
            columns: table => new
            {
                IncidentId = table.Column<long>(type: "bigint", nullable: false),
                TagId = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IncidentTags", x => new { x.IncidentId, x.TagId });
                table.ForeignKey(
                    name: "FK_IncidentTags_Incidents_IncidentId",
                    column: x => x.IncidentId,
                    principalTable: "Incidents",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_IncidentTags_Tags_TagId",
                    column: x => x.TagId,
                    principalTable: "Tags",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "IncidentUpdates",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                IncidentId = table.Column<long>(type: "bigint", nullable: false),
                AuthorId = table.Column<long>(type: "bigint", nullable: false),
                Kind = table.Column<int>(type: "integer", nullable: false),
                Body = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IncidentUpdates", x => x.Id);
                table.ForeignKey(
                    name: "FK_IncidentUpdates_Incidents_IncidentId",
                    column: x => x.IncidentId,
                    principalTable: "Incidents",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_IncidentUpdates_Users_AuthorId",
                    column: x => x.AuthorId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_IncidentPauses_IncidentId",
            table: "IncidentPauses",
            column: "IncidentId");

        migrationBuilder.CreateIndex(
            name: "IX_Incidents_AssigneeId",
            table: "Incidents",
            column: "AssigneeId");

        migrationBuilder.CreateIndex(
            name: "IX_Incidents_Number",
            table: "Incidents",
            column: "Number",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Incidents_ServiceId",
            table: "Incidents",
            column: "ServiceId");

        migrationBuilder.CreateIndex(
            name: "IX_IncidentStatusChanges_ChangedById",
            table: "IncidentStatusChanges",
            column: "ChangedById");

        migrationBuilder.CreateIndex(
            name: "IX_IncidentStatusChanges_IncidentId",
            table: "IncidentStatusChanges",
            column: "IncidentId");

        migrationBuilder.CreateIndex(
            name: "IX_IncidentTags_TagId",
            table: "IncidentTags",
            column: "TagId");

        migrationBuilder.CreateIndex(
            name: "IX_IncidentUpdates_AuthorId",
            table: "IncidentUpdates",
            column: "AuthorId");

        migrationBuilder.CreateIndex(
            name: "IX_IncidentUpdates_IncidentId",
            table: "IncidentUpdates",
            column: "IncidentId");

        migrationBuilder.CreateIndex(
            name: "IX_Services_SlaPolicyId",
            table: "Services",
            column: "SlaPolicyId");

        migrationBuilder.CreateIndex(
            name: "IX_Services_Slug",
            table: "Services",
            column: "Slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Services_TeamId",
            table: "Services",
            column: "TeamId");

        migrationBuilder.CreateIndex(
            name: "IX_SlaPolicies_Name",
            table: "SlaPolicies",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Tags_Slug",
            table: "Tags",
            column: "Slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Teams_Slug",
            table: "Teams",
            column: "Slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DatasetMetadata");

        migrationBuilder.DropTable(
            name: "IncidentPauses");

        migrationBuilder.DropTable(
            name: "IncidentStatusChanges");

        migrationBuilder.DropTable(
            name: "IncidentTags");

        migrationBuilder.DropTable(
            name: "IncidentUpdates");

        migrationBuilder.DropTable(
            name: "SlaTargets");

        migrationBuilder.DropTable(
            name: "Tags");

        migrationBuilder.DropTable(
            name: "Incidents");

        migrationBuilder.DropTable(
            name: "Services");

        migrationBuilder.DropTable(
            name: "Users");

        migrationBuilder.DropTable(
            name: "SlaPolicies");

        migrationBuilder.DropTable(
            name: "Teams");
    }
}
