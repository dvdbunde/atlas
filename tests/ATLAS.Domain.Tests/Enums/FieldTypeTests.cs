using ATLAS.Domain.Enums;
using Xunit;

namespace ATLAS.Domain.Tests.Enums
{
    public class FieldTypeTests
    {
        [Fact]
        public void FieldType_ShouldHaveTextValueZero()
        {
            Assert.Equal(0, (int)FieldType.Text);
        }

        [Fact]
        public void FieldType_ShouldHaveFileUploadValueSix()
        {
            Assert.Equal(6, (int)FieldType.FileUpload);
        }

        [Fact]
        public void FieldType_ShouldContainFileUpload()
        {
            Assert.Contains(FieldType.FileUpload, System.Enum.GetValues<FieldType>());
        }

        [Fact]
        public void FieldType_ShouldPreserveAllExistingValues()
        {
            Assert.Equal(0, (int)FieldType.Text);
            Assert.Equal(1, (int)FieldType.MultilineText);
            Assert.Equal(2, (int)FieldType.Number);
            Assert.Equal(3, (int)FieldType.Date);
            Assert.Equal(4, (int)FieldType.Boolean);
            Assert.Equal(5, (int)FieldType.Dropdown);
            Assert.Equal(6, (int)FieldType.FileUpload);
        }
    }
}