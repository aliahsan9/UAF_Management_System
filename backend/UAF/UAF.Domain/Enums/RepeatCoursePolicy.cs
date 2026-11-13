namespace UAF.Domain.Enums;
/// <summary>How repeated attempts for the same course contribute to GPA calculations.</summary>
public enum RepeatCoursePolicy
{
    AllAttempts = 1,
    LatestAttempt = 2,
    HighestGrade = 3
}
