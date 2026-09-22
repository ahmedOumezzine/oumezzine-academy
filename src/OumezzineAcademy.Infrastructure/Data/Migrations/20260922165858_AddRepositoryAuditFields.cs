using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OumezzineAcademy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRepositoryAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                table: "QuizQuestions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                table: "QuizAnswers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                table: "LearningPaths",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                table: "LearningPathCourses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                table: "LearningPathCategories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                table: "Courses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                table: "CourseQuizzes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                table: "CourseLessons",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                table: "CourseContents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                table: "CourseCategories",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                table: "QuizQuestions");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                table: "QuizAnswers");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                table: "LearningPaths");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                table: "LearningPathCourses");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                table: "LearningPathCategories");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                table: "CourseQuizzes");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                table: "CourseLessons");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                table: "CourseContents");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                table: "CourseCategories");
        }
    }
}