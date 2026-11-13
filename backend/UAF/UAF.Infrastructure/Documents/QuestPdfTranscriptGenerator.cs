using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using UAF.Application.Features.Grading.Models;
using UAF.Application.Features.Transcripts.Abstractions;
namespace UAF.Infrastructure.Documents;
internal sealed class QuestPdfTranscriptGenerator : ITranscriptPdfGenerator
{
    public QuestPdfTranscriptGenerator() => QuestPDF.Settings.License = LicenseType.Community;
    public byte[] Generate(StudentAcademicRecordResponse record, string verificationCode, string snapshotSha256)
    {
        ArgumentNullException.ThrowIfNull(record);
        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(9));
            page.Header().Column(header =>
            {
                header.Item().AlignCenter().Text("UNIVERSITY ACADEMIC TRANSCRIPT").FontSize(17).Bold();
                header.Item().AlignCenter().Text("Academic record snapshot").FontSize(9);
            });
            page.Content().Column(column =>
            {
                column.Spacing(7);
                column.Item().PaddingTop(8).Text($"Student: {record.FullName}").Bold();
                column.Item().Text($"Registration number: {record.RegistrationNumber}");
                column.Item().Text($"Issued snapshot includes {record.CourseAttempts.Count} finalized course attempt(s).");
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.0f);
                        columns.RelativeColumn(1.1f);
                        columns.RelativeColumn(2.4f);
                        columns.RelativeColumn(0.65f);
                        columns.RelativeColumn(0.8f);
                        columns.RelativeColumn(0.75f);
                        columns.RelativeColumn(0.7f);
                    });
                    table.Header(header =>
                    {
                        Header(header.Cell(), "Session");
                        Header(header.Cell(), "Code");
                        Header(header.Cell(), "Course");
                        Header(header.Cell(), "Credit");
                        Header(header.Cell(), "Marks");
                        Header(header.Cell(), "Grade");
                        Header(header.Cell(), "Points");
                    });
                    foreach (var course in record.CourseAttempts)
                    {
                        Cell(table.Cell(), course.AcademicSessionName);
                        Cell(table.Cell(), course.CourseCode);
                        Cell(table.Cell(), course.CourseTitle);
                        Cell(table.Cell(), course.CreditHours.ToString("0.##"));
                        Cell(table.Cell(), $"{course.MarksObtained:0.##}/{course.TotalMarks:0.##}");
                        Cell(table.Cell(), course.LetterGrade);
                        Cell(table.Cell(), course.GradePoints.ToString("0.00"));
                    }
                });
                column.Item().PaddingTop(8).Text($"Cumulative GPA: {(record.CumulativeGpa.HasValue ? record.CumulativeGpa.Value.ToString("0.00") : "Not available")}");
                column.Item().Text($"GPA credit hours: {record.CumulativeGpaCreditHours:0.##}");
                column.Item().PaddingTop(6).Text("Semester summary").Bold();
                foreach (var semester in record.Semesters)
                    column.Item().Text($"{semester.AcademicSessionName}: {(semester.Gpa.HasValue ? semester.Gpa.Value.ToString("0.00") : "Not available")} GPA ({semester.GpaCreditHours:0.##} credits)");
                column.Item().PaddingTop(10).Text("This document represents the immutable academic record snapshot stored at issuance time.");
            });
            page.Footer().Column(footer =>
            {
                footer.Item().AlignCenter().Text($"Verification code: {verificationCode}").FontSize(8).Bold();
                footer.Item().AlignCenter().Text($"Snapshot SHA-256: {snapshotSha256}").FontSize(6);
            });
        })).GeneratePdf();
    }
    private static void Header(IContainer container, string text) =>
        container.Background(Colors.Grey.Lighten2).Padding(4).Text(text).Bold().FontSize(7);
    private static void Cell(IContainer container, string text) =>
        container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(text).FontSize(7);
}
