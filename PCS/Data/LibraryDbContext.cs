using Microsoft.EntityFrameworkCore;
using PCS.Models;

namespace PCS.Data;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Genre> Genres => Set<Genre>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Author>(entity =>
        {
            entity.HasKey(author => author.Id);

            entity.Property(author => author.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(author => author.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(author => author.Country)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(author => author.BirthDate)
                .IsRequired();
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.HasKey(genre => genre.Id);

            entity.Property(genre => genre.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(genre => genre.Description)
                .HasMaxLength(500);
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(book => book.Id);

            entity.Property(book => book.Title)
                .IsRequired()
                .HasMaxLength(250);

            entity.Property(book => book.ISBN)
                .IsRequired()
                .HasMaxLength(13);

            entity.Property(book => book.PublishYear)
                .IsRequired();

            entity.Property(book => book.QuantityInStock)
                .IsRequired();

            entity.HasIndex(book => book.ISBN)
                .IsUnique();

            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Books_QuantityInStock", "\"QuantityInStock\" >= 0");
                table.HasCheckConstraint("CK_Books_PublishYear", "\"PublishYear\" >= 1450");
            });
        });

        modelBuilder.Entity<Book>()
            .HasMany(book => book.Authors)
            .WithMany(author => author.Books)
            .UsingEntity<Dictionary<string, object>>(
                "BookAuthor",
                right => right.HasOne<Author>().WithMany().HasForeignKey("AuthorId").OnDelete(DeleteBehavior.Cascade),
                left => left.HasOne<Book>().WithMany().HasForeignKey("BookId").OnDelete(DeleteBehavior.Cascade),
                join => join.HasKey("BookId", "AuthorId"));

        modelBuilder.Entity<Book>()
            .HasMany(book => book.Genres)
            .WithMany(genre => genre.Books)
            .UsingEntity<Dictionary<string, object>>(
                "BookGenre",
                right => right.HasOne<Genre>().WithMany().HasForeignKey("GenreId").OnDelete(DeleteBehavior.Cascade),
                left => left.HasOne<Book>().WithMany().HasForeignKey("BookId").OnDelete(DeleteBehavior.Cascade),
                join => join.HasKey("BookId", "GenreId"));
    }
}

