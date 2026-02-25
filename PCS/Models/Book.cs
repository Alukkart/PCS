namespace PCS.Models;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int PublishYear { get; set; }
    public string ISBN { get; set; } = string.Empty;
    public int QuantityInStock { get; set; }

    public ICollection<Author> Authors { get; set; } = new List<Author>();
    public ICollection<Genre> Genres { get; set; } = new List<Genre>();

    public string AuthorNames => string.Join(", ", Authors
        .OrderBy(author => author.LastName)
        .ThenBy(author => author.FirstName)
        .Select(author => author.FullName));

    public string GenreNames => string.Join(", ", Genres
        .OrderBy(genre => genre.Name)
        .Select(genre => genre.Name));
}

