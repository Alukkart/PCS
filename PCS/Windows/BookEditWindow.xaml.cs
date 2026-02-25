using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using PCS.Data;
using PCS.Models;

namespace PCS.Windows;

public partial class BookEditWindow : Window
{
    private readonly int? _bookId;

    public BookEditWindow(int? bookId = null)
    {
        _bookId = bookId;
        Title = _bookId.HasValue ? "Редактирование книги" : "Добавление книги";

        InitializeComponent();
        Loaded += BookEditWindow_OnLoaded;
    }

    private void BookEditWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        using var dbContext = LibraryDbContextFactory.Create();

        var authors = dbContext.Authors
            .OrderBy(author => author.LastName)
            .ThenBy(author => author.FirstName)
            .ToList();

        var genres = dbContext.Genres
            .OrderBy(genre => genre.Name)
            .ToList();

        AuthorsListBox.ItemsSource = authors;
        GenresListBox.ItemsSource = genres;

        if (authors.Count == 0 || genres.Count == 0)
        {
            SaveButton.IsEnabled = false;

            MessageBox.Show(
                "Перед добавлением книги создайте минимум одного автора и один жанр.",
                "Недостаточно данных",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (_bookId.HasValue)
        {
            var book = dbContext.Books
                .Include(entity => entity.Authors)
                .Include(entity => entity.Genres)
                .FirstOrDefault(entity => entity.Id == _bookId.Value);

            if (book is null)
            {
                MessageBox.Show(
                    "Книга не найдена. Возможно, она уже была удалена.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Close();
                return;
            }

            TitleTextBox.Text = book.Title;
            PublishYearTextBox.Text = book.PublishYear.ToString();
            IsbnTextBox.Text = book.ISBN;
            QuantityTextBox.Text = book.QuantityInStock.ToString();

            SelectItemsByIds(AuthorsListBox, book.Authors.Select(author => author.Id).ToHashSet());
            SelectItemsByIds(GenresListBox, book.Genres.Select(genre => genre.Id).ToHashSet());
            return;
        }

        PublishYearTextBox.Text = DateTime.Now.Year.ToString();
        QuantityTextBox.Text = "1";

        AuthorsListBox.SelectedIndex = 0;
        GenresListBox.SelectedIndex = 0;
    }

    private static void SelectItemsByIds(ListBox listBox, HashSet<int> selectedIds)
    {
        listBox.SelectedItems.Clear();

        foreach (var item in listBox.Items)
        {
            var id = item switch
            {
                Author author => author.Id,
                Genre genre => genre.Id,
                _ => 0
            };

            if (selectedIds.Contains(id))
            {
                listBox.SelectedItems.Add(item);
            }
        }
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryReadInput(
                out var title,
                out var publishYear,
                out var normalizedIsbn,
                out var quantityInStock,
                out var selectedAuthorIds,
                out var selectedGenreIds))
        {
            return;
        }

        using var dbContext = LibraryDbContextFactory.Create();

        Book bookEntity;
        if (_bookId.HasValue)
        {
            var existingBook = dbContext.Books
                .Include(book => book.Authors)
                .Include(book => book.Genres)
                .FirstOrDefault(entity => entity.Id == _bookId.Value);

            if (existingBook is null)
            {
                MessageBox.Show(
                    "Книга не найдена. Возможно, она уже была удалена.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            bookEntity = existingBook;
        }
        else
        {
            bookEntity = new Book();
            dbContext.Books.Add(bookEntity);
        }

        var selectedAuthors = dbContext.Authors
            .Where(author => selectedAuthorIds.Contains(author.Id))
            .ToList();

        var selectedGenres = dbContext.Genres
            .Where(genre => selectedGenreIds.Contains(genre.Id))
            .ToList();

        bookEntity.Title = title;
        bookEntity.PublishYear = publishYear;
        bookEntity.ISBN = normalizedIsbn;
        bookEntity.QuantityInStock = quantityInStock;
        bookEntity.Authors = selectedAuthors;
        bookEntity.Genres = selectedGenres;

        try
        {
            dbContext.SaveChanges();
            DialogResult = true;
        }
        catch (DbUpdateException)
        {
            MessageBox.Show(
                "Не удалось сохранить книгу. Убедитесь, что ISBN уникален.",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Не удалось сохранить книгу.\n{exception.Message}",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private bool TryReadInput(
        out string title,
        out int publishYear,
        out string normalizedIsbn,
        out int quantityInStock,
        out List<int> authorIds,
        out List<int> genreIds)
    {
        title = TitleTextBox.Text.Trim();
        publishYear = 0;
        normalizedIsbn = string.Empty;
        quantityInStock = 0;
        authorIds = [];
        genreIds = [];

        if (string.IsNullOrWhiteSpace(title))
        {
            MessageBox.Show("Введите название книги.", "Валидация", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        if (!int.TryParse(PublishYearTextBox.Text.Trim(), out publishYear))
        {
            MessageBox.Show("Год публикации должен быть целым числом.", "Валидация", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        var currentYear = DateTime.Now.Year;
        if (publishYear < 1450 || publishYear > currentYear + 1)
        {
            MessageBox.Show(
                $"Год публикации должен быть в диапазоне 1450..{currentYear + 1}.",
                "Валидация",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return false;
        }

        var rawIsbn = IsbnTextBox.Text.Trim();
        if (!TryNormalizeAndValidateIsbn(rawIsbn, out normalizedIsbn))
        {
            MessageBox.Show(
                "Введите корректный ISBN-10 или ISBN-13 (можно с дефисами и пробелами).",
                "Валидация",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return false;
        }

        if (!int.TryParse(QuantityTextBox.Text.Trim(), out quantityInStock) || quantityInStock < 0)
        {
            MessageBox.Show(
                "Количество в наличии должно быть неотрицательным целым числом.",
                "Валидация",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return false;
        }

        authorIds = AuthorsListBox.SelectedItems
            .OfType<Author>()
            .Select(author => author.Id)
            .Distinct()
            .ToList();

        if (authorIds.Count == 0)
        {
            MessageBox.Show("Выберите хотя бы одного автора.", "Валидация", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        genreIds = GenresListBox.SelectedItems
            .OfType<Genre>()
            .Select(genre => genre.Id)
            .Distinct()
            .ToList();

        if (genreIds.Count == 0)
        {
            MessageBox.Show("Выберите хотя бы один жанр.", "Валидация", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        return true;
    }

    private static bool TryNormalizeAndValidateIsbn(string rawIsbn, out string normalizedIsbn)
    {
        normalizedIsbn = string.Concat(rawIsbn
            .Where(character => !char.IsWhiteSpace(character) && character != '-'))
            .ToUpperInvariant();

        if (normalizedIsbn.Length == 10)
        {
            return IsValidIsbn10(normalizedIsbn);
        }

        if (normalizedIsbn.Length == 13)
        {
            return IsValidIsbn13(normalizedIsbn);
        }

        return false;
    }

    private static bool IsValidIsbn10(string isbn)
    {
        if (!isbn.Take(9).All(char.IsDigit))
        {
            return false;
        }

        var lastCharacter = isbn[9];
        if (!char.IsDigit(lastCharacter) && lastCharacter != 'X')
        {
            return false;
        }

        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            sum += (i + 1) * (isbn[i] - '0');
        }

        sum += 10 * (lastCharacter == 'X' ? 10 : lastCharacter - '0');
        return sum % 11 == 0;
    }

    private static bool IsValidIsbn13(string isbn)
    {
        if (!isbn.All(char.IsDigit))
        {
            return false;
        }

        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            var digit = isbn[i] - '0';
            sum += i % 2 == 0 ? digit : digit * 3;
        }

        var expectedCheckDigit = (10 - (sum % 10)) % 10;
        var actualCheckDigit = isbn[12] - '0';

        return expectedCheckDigit == actualCheckDigit;
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}




