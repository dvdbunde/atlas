using ATLAS.Domain.Entities;
using Xunit;

namespace ATLAS.Domain.Tests.Entities;

public class BaseEntityTests
{
    [Fact]
    public void BaseEntity_Should_Have_Unique_Id()
    {
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();
        
        Assert.NotEqual(entity1.Id, entity2.Id);
        Assert.NotEqual(Guid.Empty, entity1.Id);
    }
    
    [Fact]
    public void BaseEntity_Should_Set_CreatedAt()
    {
        var before = DateTime.UtcNow;
        var entity = new TestEntity();
        var after = DateTime.UtcNow;
        
        Assert.True(entity.CreatedAt >= before);
        Assert.True(entity.CreatedAt <= after);
    }
    
    [Fact]
    public void BaseEntity_Should_Have_Null_UpdatedAt_Initially()
    {
        var entity = new TestEntity();
        Assert.Null(entity.UpdatedAt);
    }

    [Fact]
    public void BaseEntity_Should_Set_UpdatedAt_When_Modified()
    {
        var entity = new TestEntity();
        
        entity.SetUpdated();
        
        Assert.NotNull(entity.UpdatedAt);
    }
}

public class TestEntity : BaseEntity
{
}
