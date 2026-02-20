using System.Windows;
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

        AuthorComboBox.ItemsSource = authors;
        GenreComboBox.ItemsSource = genres;

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
            var book = dbContext.Books.FirstOrDefault(entity => entity.Id == _bookId.Value);
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
            AuthorComboBox.SelectedValue = book.AuthorId;
            GenreComboBox.SelectedValue = book.GenreId;
            return;
        }

        PublishYearTextBox.Text = DateTime.Now.Year.ToString();
        QuantityTextBox.Text = "1";
        AuthorComboBox.SelectedIndex = 0;
        GenreComboBox.SelectedIndex = 0;
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryReadInput(
                out var title,
                out var publishYear,
                out var isbn,
                out var quantityInStock,
                out var authorId,
                out var genreId))
        {
            return;
        }

        using var dbContext = LibraryDbContextFactory.Create();

        Book bookEntity;
        if (_bookId.HasValue)
        {
            var existingBook = dbContext.Books.FirstOrDefault(entity => entity.Id == _bookId.Value);
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

        bookEntity.Title = title;
        bookEntity.PublishYear = publishYear;
        bookEntity.ISBN = isbn;
        bookEntity.QuantityInStock = quantityInStock;
        bookEntity.AuthorId = authorId;
        bookEntity.GenreId = genreId;

        try
        {
            dbContext.SaveChanges();
            DialogResult = true;
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
        out string isbn,
        out int quantityInStock,
        out int authorId,
        out int genreId)
    {
        title = TitleTextBox.Text.Trim();
        isbn = IsbnTextBox.Text.Trim();
        publishYear = 0;
        quantityInStock = 0;
        authorId = 0;
        genreId = 0;

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

        if (string.IsNullOrWhiteSpace(isbn))
        {
            MessageBox.Show("Введите ISBN.", "Валидация", MessageBoxButton.OK, MessageBoxImage.Information);
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

        if (AuthorComboBox.SelectedValue is not int selectedAuthorId)
        {
            MessageBox.Show("Выберите автора.", "Валидация", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        if (GenreComboBox.SelectedValue is not int selectedGenreId)
        {
            MessageBox.Show("Выберите жанр.", "Валидация", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        authorId = selectedAuthorId;
        genreId = selectedGenreId;
        return true;
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
