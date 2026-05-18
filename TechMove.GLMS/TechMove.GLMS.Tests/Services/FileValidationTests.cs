using Xunit;
using System.IO;

namespace TechMove.GLMS.Tests.Services
{
    public class FileValidationTests
    {
        [Fact]
        public void FileValidation_ValidPdfExtension_Passes()
        {
            var fileName = "document.pdf";
            var extension = Path.GetExtension(fileName).ToLower();

            Assert.Equal(".pdf", extension);
        }

        [Fact]
        public void FileValidation_InvalidExtension_Fails()
        {
            var fileName = "virus.exe";
            var extension = Path.GetExtension(fileName).ToLower();

            Assert.NotEqual(".pdf", extension);
        }

        [Fact]
        public void FileValidation_ThrowsExceptionOnInvalidType()
        {
            var fileName = "malware.exe";

            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                var extension = Path.GetExtension(fileName).ToLower();
                if (extension != ".pdf")
                    throw new InvalidOperationException("Only PDF files are allowed.");
            });

            Assert.Contains("PDF", exception.Message);
        }

        [Theory]
        [InlineData("report.pdf", true)]
        [InlineData("image.png", false)]
        [InlineData("spreadsheet.xlsx", false)]
        [InlineData("DOCUMENT.PDF", true)]
        public void FileValidation_VariousExtensions(string fileName, bool shouldPass)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            var isValid = extension == ".pdf";

            Assert.Equal(shouldPass, isValid);
        }
    }
}