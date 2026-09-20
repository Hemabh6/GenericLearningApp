using GenericLearningApp.Domain;
using GenericLearningApp.Domain.Entities;
using GenericLearningApp.Domain.Enums;

namespace GenericLearningApp.Domain.Tests;

public class ContentTreeTests
{
    private readonly Guid _subjectId = Guid.NewGuid();

    [Fact]
    public void Root_node_is_depth_zero_and_children_nest_without_limit()
    {
        var maths = new ContentNode(_subjectId, "Math");
        var algebra = new ContentNode(_subjectId, "Linear algebra", maths);
        var vectors = new ContentNode(_subjectId, "Vectors", algebra);

        Assert.Equal(0, maths.Depth);
        Assert.Equal(1, algebra.Depth);
        Assert.Equal(2, vectors.Depth);
        Assert.Contains(maths.Id.ToString(), vectors.MaterializedPath);
    }

    [Fact]
    public void A_node_cannot_become_its_own_parent()
    {
        var node = new ContentNode(_subjectId, "Math");
        Assert.Throws<DomainException>(() => node.AttachTo(node));
    }

    [Fact]
    public void A_node_cannot_move_beneath_its_own_descendant()
    {
        var maths = new ContentNode(_subjectId, "Math");
        var algebra = new ContentNode(_subjectId, "Linear algebra", maths);

        Assert.Throws<DomainException>(() => maths.AttachTo(algebra));
    }

    [Fact]
    public void A_node_cannot_move_to_another_subject()
    {
        var mine = new ContentNode(_subjectId, "Math");
        var theirs = new ContentNode(Guid.NewGuid(), "Physics");

        Assert.Throws<DomainException>(() => mine.AttachTo(theirs));
    }

    [Fact]
    public void An_item_with_no_section_is_allowed_and_lands_ungrouped()
    {
        var item = new ContentItem(_subjectId, "Some talk", ContentKind.Video, "https://example.com/v");

        Assert.Null(item.NodeId);
        Assert.Equal("https://example.com/v", item.Url);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://example.com/file")]
    [InlineData("javascript:alert(1)")]
    public void An_item_link_must_be_http_or_https(string url)
        => Assert.Throws<DomainException>(() => new ContentItem(_subjectId, "Bad link", ContentKind.Video, url));

    [Fact]
    public void Blank_links_are_stored_as_null_rather_than_empty_text()
    {
        var item = new ContentItem(_subjectId, "A book", ContentKind.Book, "   ");
        Assert.Null(item.Url);
    }

    [Fact]
    public void Ticking_an_item_records_when_it_was_finished()
    {
        var item = new ContentItem(_subjectId, "Chapter 1", ContentKind.Article);

        item.SetDone(true);
        Assert.NotNull(item.CompletedAt);

        item.SetDone(false);
        Assert.Null(item.CompletedAt);
    }
}
