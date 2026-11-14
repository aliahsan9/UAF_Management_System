namespace UAF.Application.Features.Documents;
public sealed record FeeChallanPdfLine(string Code,string Name,decimal Amount);
public interface IUniversityDocumentPdfGenerator{
 byte[] GenerateAdmissionLetter(string universityName,string applicationNumber,string applicantName,string programName,DateOnly issueDate);
 byte[] GenerateFeeChallan(string universityName,string challanNumber,string studentName,string registrationNumber,DateOnly issueDate,DateOnly dueDate,string currency,decimal subtotal,decimal lateFee,IReadOnlyList<FeeChallanPdfLine> lines);
}
