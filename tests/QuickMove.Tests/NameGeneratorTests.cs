namespace QuickMove.Tests;

public class NameGeneratorTests
{
    static HashSet<string> Names(params string[] names) =>
        new(names, StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void Free_name_is_kept()
    {
        Assert.Equal("报告.pdf", NameGenerator.Choose("报告.pdf", false, Names()));
    }

    [Fact]
    public void File_uses_last_extension()
    {
        Assert.Equal("报告 (1).pdf", NameGenerator.Choose("报告.pdf", false, Names("报告.pdf")));
        Assert.Equal("archive.tar (1).gz", NameGenerator.Choose("archive.tar.gz", false, Names("archive.tar.gz")));
    }

    [Fact]
    public void Directory_does_not_split_extension()
    {
        Assert.Equal("资料 (1)", NameGenerator.Choose("资料", true, Names("资料")));
        Assert.Equal("资料.tar (1)", NameGenerator.Choose("资料.tar", true, Names("资料.tar")));
    }

    [Fact]
    public void Occupied_lower_numbers_are_skipped()
    {
        var occupied = Names("a.txt", "a (1).txt", "a (2).TXT");
        Assert.Equal("a (3).txt", NameGenerator.Choose("a.txt", false, occupied));
    }

    [Fact]
    public void Existing_suffix_in_the_source_name_is_kept()
    {
        Assert.Equal("报告 (1) (1).pdf", NameGenerator.Choose("报告 (1).pdf", false, Names("报告 (1).pdf")));
    }

    [Fact]
    public void All_999_suffixes_occupied_returns_null()
    {
        var occupied = Names("a.txt");
        for (var i = 1; i <= 999; i++)
            occupied.Add($"a ({i}).txt");
        Assert.Null(NameGenerator.Choose("a.txt", false, occupied));
    }
}
