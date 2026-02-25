using System.Data;
using Microsoft.EntityFrameworkCore;
using PCS.Models;

namespace PCS.Data;

public static class LibraryDbContextFactory
{
    public static LibraryDbContext Create()
    {
        var optionsBuilder = new DbContextOptionsBuilder<LibraryDbContext>();
        optionsBuilder.UseSqlite(DatabaseSettings.ConnectionString);
        return new LibraryDbContext(optionsBuilder.Options);
    }

    public static void InitializeDatabase()
    {
        using var dbContext = Create();
        dbContext.Database.EnsureCreated();

        MigrateLegacyBookRelations(dbContext);
        SeedBaseData(dbContext);
    }

    private static void MigrateLegacyBookRelations(LibraryDbContext dbContext)
    {
        var hasBookAuthorTable = TableExists(dbContext, "BookAuthor");
        var hasBookGenreTable = TableExists(dbContext, "BookGenre");

        if (!hasBookAuthorTable)
        {
            dbContext.Database.ExecuteSqlRaw(
                """
                CREATE TABLE IF NOT EXISTS "BookAuthor" (
                    "BookId" INTEGER NOT NULL,
                    "AuthorId" INTEGER NOT NULL,
                    CONSTRAINT "PK_BookAuthor" PRIMARY KEY ("BookId", "AuthorId"),
                    CONSTRAINT "FK_BookAuthor_Books_BookId" FOREIGN KEY ("BookId") REFERENCES "Books" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_BookAuthor_Authors_AuthorId" FOREIGN KEY ("AuthorId") REFERENCES "Authors" ("Id") ON DELETE CASCADE
                );
                """);

            dbContext.Database.ExecuteSqlRaw(
                "CREATE INDEX IF NOT EXISTS \"IX_BookAuthor_AuthorId\" ON \"BookAuthor\" (\"AuthorId\");");

            if (ColumnExists(dbContext, "Books", "AuthorId"))
            {
                dbContext.Database.ExecuteSqlRaw(
                    """
                    INSERT OR IGNORE INTO "BookAuthor" ("BookId", "AuthorId")
                    SELECT "Id", "AuthorId"
                    FROM "Books"
                    WHERE "AuthorId" IS NOT NULL;
                    """);
            }
        }

        if (!hasBookGenreTable)
        {
            dbContext.Database.ExecuteSqlRaw(
                """
                CREATE TABLE IF NOT EXISTS "BookGenre" (
                    "BookId" INTEGER NOT NULL,
                    "GenreId" INTEGER NOT NULL,
                    CONSTRAINT "PK_BookGenre" PRIMARY KEY ("BookId", "GenreId"),
                    CONSTRAINT "FK_BookGenre_Books_BookId" FOREIGN KEY ("BookId") REFERENCES "Books" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_BookGenre_Genres_GenreId" FOREIGN KEY ("GenreId") REFERENCES "Genres" ("Id") ON DELETE CASCADE
                );
                """);

            dbContext.Database.ExecuteSqlRaw(
                "CREATE INDEX IF NOT EXISTS \"IX_BookGenre_GenreId\" ON \"BookGenre\" (\"GenreId\");");

            if (ColumnExists(dbContext, "Books", "GenreId"))
            {
                dbContext.Database.ExecuteSqlRaw(
                    """
                    INSERT OR IGNORE INTO "BookGenre" ("BookId", "GenreId")
                    SELECT "Id", "GenreId"
                    FROM "Books"
                    WHERE "GenreId" IS NOT NULL;
                    """);
            }
        }
    }

    private static bool TableExists(LibraryDbContext dbContext, string tableName)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = $name;";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        var result = Convert.ToInt32(command.ExecuteScalar());
        return result > 0;
    }

    private static bool ColumnExists(LibraryDbContext dbContext, string tableName, string columnName)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var currentColumnName = reader["name"]?.ToString();
            if (string.Equals(currentColumnName, columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void SeedBaseData(LibraryDbContext dbContext)
    {
        if (dbContext.Authors.Any() || dbContext.Genres.Any() || dbContext.Books.Any())
        {
            return;
        }

        var tolstoy = new Author
        {
            FirstName = "Leo",
            LastName = "Tolstoy",
            BirthDate = new DateTime(1828, 9, 9),
            Country = "Russia"
        };

        var dostoevsky = new Author
        {
            FirstName = "Fyodor",
            LastName = "Dostoevsky",
            BirthDate = new DateTime(1821, 11, 11),
            Country = "Russia"
        };

        var gaiman = new Author
        {
            FirstName = "Neil",
            LastName = "Gaiman",
            BirthDate = new DateTime(1960, 11, 10),
            Country = "United Kingdom"
        };

        var pratchett = new Author
        {
            FirstName = "Terry",
            LastName = "Pratchett",
            BirthDate = new DateTime(1948, 4, 28),
            Country = "United Kingdom"
        };

        var classic = new Genre { Name = "Classic", Description = "Classic literature" };
        var novel = new Genre { Name = "Novel", Description = "Prose fiction" };
        var fantasy = new Genre { Name = "Fantasy", Description = "Fantasy stories" };
        var satire = new Genre { Name = "Satire", Description = "Satirical literature" };

        var warAndPeace = new Book
        {
            Title = "War and Peace",
            PublishYear = 1869,
            ISBN = "9780140447934",
            QuantityInStock = 4,
            Authors = [tolstoy],
            Genres = [classic, novel]
        };

        var crimeAndPunishment = new Book
        {
            Title = "Crime and Punishment",
            PublishYear = 1866,
            ISBN = "9780140449136",
            QuantityInStock = 6,
            Authors = [dostoevsky],
            Genres = [classic, novel]
        };

        var goodOmens = new Book
        {
            Title = "Good Omens",
            PublishYear = 1990,
            ISBN = "9780060853983",
            QuantityInStock = 5,
            Authors = [gaiman, pratchett],
            Genres = [fantasy, satire]
        };

        dbContext.Books.AddRange(warAndPeace, crimeAndPunishment, goodOmens);
        dbContext.SaveChanges();
    }
}
