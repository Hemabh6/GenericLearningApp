using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenericLearningApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialLearning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "subjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "content_nodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    MaterializedPath = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_nodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_nodes_content_nodes_ParentId",
                        column: x => x.ParentId,
                        principalTable: "content_nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_content_nodes_subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notes_subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Unit = table.Column<int>(type: "integer", nullable: false),
                    Length = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_plans_subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "question_sets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_sets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_question_sets_subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reference_terms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Term = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Meaning = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    UseItWhen = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reference_terms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reference_terms_subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "content_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    IsDone = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_items_content_nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "content_nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_content_items_subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "checkpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AtUnit = table.Column<int>(type: "integer", nullable: false),
                    Shape = table.Column<int>(type: "integer", nullable: false),
                    IsDone = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checkpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_checkpoints_plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "phases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartUnit = table.Column<int>(type: "integer", nullable: false),
                    EndUnit = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_phases_plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionSetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Options = table.Column<string>(type: "jsonb", nullable: false),
                    CorrectIndex = table.Column<int>(type: "integer", nullable: false),
                    Explanation = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_questions_question_sets_QuestionSetId",
                        column: x => x.QuestionSetId,
                        principalTable: "question_sets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_attempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionSetId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    TotalQuestions = table.Column<int>(type: "integer", nullable: false),
                    Answers = table.Column<string>(type: "jsonb", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_attempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_test_attempts_question_sets_QuestionSetId",
                        column: x => x.QuestionSetId,
                        principalTable: "question_sets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plan_tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Lane = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    StartUnit = table.Column<int>(type: "integer", nullable: false),
                    EndUnit = table.Column<int>(type: "integer", nullable: false),
                    IsDone = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_plan_tasks_phases_PhaseId",
                        column: x => x.PhaseId,
                        principalTable: "phases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_plan_tasks_plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_checkpoints_PlanId_AtUnit",
                table: "checkpoints",
                columns: new[] { "PlanId", "AtUnit" });

            migrationBuilder.CreateIndex(
                name: "IX_content_items_NodeId_SortOrder",
                table: "content_items",
                columns: new[] { "NodeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_content_items_SubjectId_IsDone",
                table: "content_items",
                columns: new[] { "SubjectId", "IsDone" });

            migrationBuilder.CreateIndex(
                name: "IX_content_nodes_ParentId",
                table: "content_nodes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_content_nodes_SubjectId_MaterializedPath",
                table: "content_nodes",
                columns: new[] { "SubjectId", "MaterializedPath" });

            migrationBuilder.CreateIndex(
                name: "IX_content_nodes_SubjectId_SortOrder",
                table: "content_nodes",
                columns: new[] { "SubjectId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_notes_SubjectId_UpdatedAt",
                table: "notes",
                columns: new[] { "SubjectId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_phases_PlanId_SortOrder",
                table: "phases",
                columns: new[] { "PlanId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_plan_tasks_PhaseId",
                table: "plan_tasks",
                column: "PhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_plan_tasks_PlanId_Lane",
                table: "plan_tasks",
                columns: new[] { "PlanId", "Lane" });

            migrationBuilder.CreateIndex(
                name: "IX_plans_SubjectId",
                table: "plans",
                column: "SubjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_question_sets_SubjectId",
                table: "question_sets",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_questions_QuestionSetId_SortOrder",
                table: "questions",
                columns: new[] { "QuestionSetId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_reference_terms_SubjectId_SortOrder",
                table: "reference_terms",
                columns: new[] { "SubjectId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_subjects_UserId",
                table: "subjects",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_test_attempts_QuestionSetId_SubmittedAt",
                table: "test_attempts",
                columns: new[] { "QuestionSetId", "SubmittedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "checkpoints");

            migrationBuilder.DropTable(
                name: "content_items");

            migrationBuilder.DropTable(
                name: "notes");

            migrationBuilder.DropTable(
                name: "plan_tasks");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "reference_terms");

            migrationBuilder.DropTable(
                name: "test_attempts");

            migrationBuilder.DropTable(
                name: "content_nodes");

            migrationBuilder.DropTable(
                name: "phases");

            migrationBuilder.DropTable(
                name: "question_sets");

            migrationBuilder.DropTable(
                name: "plans");

            migrationBuilder.DropTable(
                name: "subjects");
        }
    }
}
