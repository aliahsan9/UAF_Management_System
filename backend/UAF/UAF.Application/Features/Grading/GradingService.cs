using UAF.Application.Common.Abstractions;
using UAF.Application.Features.Grading.Abstractions;
using UAF.Application.Features.Grading.Models;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
namespace UAF.Application.Features.Grading;
public sealed class GradingService
{
    private readonly IGradingRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    public GradingService(IGradingRepository repository, IUnitOfWork unitOfWork) { _repository = repository; _unitOfWork = unitOfWork; }

    public async Task<GradeScaleResponse> CreateScaleAsync(CreateGradeScaleRequest request, Guid actorId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (actorId == Guid.Empty) throw new ArgumentException("Authenticated actor is required.");
        if (request.Bands is null || request.Bands.Count == 0 || request.Bands.Count > 30) throw new ArgumentException("Provide between 1 and 30 grade bands.");
        if (!await _repository.AcademicSessionExistsAsync(request.AcademicSessionId, ct)) throw new KeyNotFoundException("Academic session not found.");
        if (await _repository.GetScaleForSessionAsync(request.AcademicSessionId, ct) is not null) throw new InvalidOperationException("A grading scale already exists for this academic session.");
        if (request.Bands.Select(x => x.LetterGrade?.Trim().ToUpperInvariant()).Distinct(StringComparer.Ordinal).Count() != request.Bands.Count ||
            request.Bands.Select(x => x.MinimumPercentage).Distinct().Count() != request.Bands.Count)
            throw new ArgumentException("Letter grades and minimum percentage thresholds must be unique.");
        if (!request.Bands.Any(x => x.MinimumPercentage == 0)) throw new ArgumentException("At least one grade band must start at 0 percent.");
        var scale = new GradeScale(request.AcademicSessionId, request.Name, request.IncludeFailedCoursesInGpa, request.RepeatCoursePolicy, actorId);
        foreach (var band in request.Bands)
            scale.AddBand(new GradeScaleBand(scale.Id, band.LetterGrade, band.MinimumPercentage, band.GradePoints, band.IsPassing));
        await _repository.AddScaleAsync(scale, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return ToResponse(scale);
    }

    public async Task<GradeScaleResponse> PublishScaleAsync(Guid scaleId, Guid actorId, CancellationToken ct)
    {
        var scale = await _repository.GetScaleAsync(scaleId, ct) ?? throw new KeyNotFoundException("Grading scale not found.");
        scale.Publish(actorId, DateTimeOffset.UtcNow);
        await _unitOfWork.SaveChangesAsync(ct);
        return ToResponse(scale);
    }

    public async Task<GradeScaleResponse> GetScaleForSessionAsync(Guid sessionId, CancellationToken ct)
    {
        var scale = await _repository.GetScaleForSessionAsync(sessionId, ct) ?? throw new KeyNotFoundException("Grading scale not found for this academic session.");
        return ToResponse(scale);
    }

    public async Task<CourseGradeResponse> FinalizeCourseGradeAsync(Guid offeringId, Guid studentId, FinalizeCourseGradeRequest request, Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (studentId == Guid.Empty || actorId == Guid.Empty) throw new ArgumentException("Student and authenticated actor are required.");
        if (request.TotalMarks <= 0 || request.MarksObtained < 0 || request.MarksObtained > request.TotalMarks) throw new ArgumentOutOfRangeException(nameof(request), "Marks must be valid and within total marks.");
        if (!await _repository.CanFinalizeGradeAsync(offeringId, actorId, privileged, departmentAdmin, ct))
            throw new UnauthorizedAccessException("You cannot finalize grades for this course offering.");
        var source = await _repository.GetCourseGradeSourceAsync(offeringId, studentId, ct) ?? throw new KeyNotFoundException("Eligible student enrollment or course offering not found.");
        if (await _repository.CourseGradeExistsAsync(offeringId, studentId, ct)) throw new InvalidOperationException("A finalized course grade already exists for this student and offering.");
        var scale = await _repository.GetScaleForSessionAsync(source.AcademicSessionId, ct) ?? throw new InvalidOperationException("Configure a grading scale before finalizing course grades.");
        if (!scale.IsPublished) throw new InvalidOperationException("The academic session grading scale must be published before grade finalization.");
        var percentage = decimal.Round(request.MarksObtained / request.TotalMarks * 100m, 2, MidpointRounding.AwayFromZero);
        var band = scale.Bands.Where(x => x.MinimumPercentage <= percentage).OrderByDescending(x => x.MinimumPercentage).FirstOrDefault()
            ?? throw new InvalidOperationException("The published grading scale does not cover this percentage.");
        var now = DateTimeOffset.UtcNow;
        var grade = new StudentCourseGrade(source.StudentId, source.CourseOfferingId, source.CourseId, source.AcademicSessionId, scale.Id,
            source.CourseCode, source.CourseTitle, source.CreditHours, request.TotalMarks, request.MarksObtained, percentage,
            band.LetterGrade, band.GradePoints, band.IsPassing, scale.IncludeFailedCoursesInGpa, scale.RepeatCoursePolicy, actorId, now);
        await _repository.FinalizeCourseGradeAsync(grade, ct);
        return new CourseGradeResponse(grade.Id, source.StudentId, source.RegistrationNumber, offeringId, source.CourseCode, source.CourseTitle,
            source.CreditHours, request.TotalMarks, request.MarksObtained, percentage, band.LetterGrade, band.GradePoints, band.IsPassing, now);
    }

    public async Task<StudentAcademicRecordResponse> GetMyAcademicRecordAsync(Guid actorId, CancellationToken ct)
    {
        if (actorId == Guid.Empty) throw new ArgumentException("Authenticated actor is required.");
        var studentId = await _repository.GetStudentIdForIdentityUserAsync(actorId, ct) ?? throw new KeyNotFoundException("No student record is linked to this account.");
        var source = await _repository.GetAcademicRecordAsync(studentId, ct) ?? throw new KeyNotFoundException("Student record not found.");
        return BuildAcademicRecord(source);
    }

    private static StudentAcademicRecordResponse BuildAcademicRecord(StudentAcademicRecordSource source)
    {
        var sessions = source.CourseGrades.GroupBy(x => new { x.AcademicSessionId, x.AcademicSessionName, x.AcademicSessionStartDate })
            .OrderBy(x => x.Key.AcademicSessionStartDate)
            .Select(group =>
            {
                var policy = group.OrderByDescending(x => x.FinalizedAtUtc).First();
                var result = GpaCalculator.Calculate(group, policy.RepeatCoursePolicy, policy.IncludeFailedCoursesInGpa);
                return new SemesterGpaResponse(group.Key.AcademicSessionId, group.Key.AcademicSessionName, result.Gpa ?? 0m, result.CreditHours, group.OrderBy(x => x.CourseCode).ToArray());
            }).ToArray();
        decimal? cgpa = null;
        decimal totalCredits = 0;
        if (source.CourseGrades.Count > 0)
        {
            var currentPolicy = source.CourseGrades.OrderByDescending(x => x.AcademicSessionStartDate).ThenByDescending(x => x.FinalizedAtUtc).First();
            var result = GpaCalculator.Calculate(source.CourseGrades, currentPolicy.RepeatCoursePolicy, currentPolicy.IncludeFailedCoursesInGpa);
            cgpa = result.Gpa;
            totalCredits = result.CreditHours;
        }
        return new StudentAcademicRecordResponse(source.StudentId, source.RegistrationNumber, source.FullName, sessions, cgpa, totalCredits,
            source.CourseGrades.OrderBy(x => x.AcademicSessionStartDate).ThenBy(x => x.CourseCode).ToArray());
    }
    private static GradeScaleResponse ToResponse(GradeScale scale) => new(scale.Id, scale.AcademicSessionId, scale.Name, scale.IncludeFailedCoursesInGpa,
        scale.RepeatCoursePolicy, scale.IsPublished, scale.Bands.OrderByDescending(x => x.MinimumPercentage)
            .Select(x => new GradeScaleBandResponse(x.LetterGrade, x.MinimumPercentage, x.GradePoints, x.IsPassing)).ToArray());
}
