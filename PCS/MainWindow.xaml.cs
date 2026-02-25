using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using PCS.Data;
using PCS.Models;
using PCS.Windows;

namespace PCS;

public partial class MainWindow : Window
{
    private List<Book> _allBooks = [];

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_OnLoaded;
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        ReloadData();
    }

    private void ReloadData()
    {
        using var dbContext = LibraryDbContextFactory.Create();

        var authors = dbContext.Authors
            .AsNoTracking()
            .OrderBy(author => author.LastName)
            .ThenBy(author => author.FirstName)
            .ToList();

        var genres = dbContext.Genres
            .AsNoTracking()
            .OrderBy(genre => genre.Name)
            .ToList();

        _allBooks = dbContext.Books
            .AsNoTracking()
            .Include(book => book.Authors)
            .Include(book => book.Genres)
            .OrderBy(book => book.Title)
            .ToList();

        AuthorFilterComboBox.ItemsSource = BuildFilterItems(
            authors.Select(author => new FilterItem(author.Id, author.FullName)),
            "Все авторы");

        GenreFilterComboBox.ItemsSource = BuildFilterItems(
            genres.Select(genre => new FilterItem(genre.Id, genre.Name)),
            "Все жанры");

        AuthorFilterComboBox.SelectedIndex = 0;
        GenreFilterComboBox.SelectedIndex = 0;

        ApplyFilters();
    }

    private static List<FilterItem> BuildFilterItems(IEnumerable<FilterItem> filterItems, string defaultName)
    {
        var result = new List<FilterItem> { new(null, defaultName) };
        result.AddRange(filterItems);
        return result;
    }

    private void ApplyFilters()
    {
        IEnumerable<Book> filteredQuery = _allBooks;

        var searchText = SearchTextBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            filteredQuery = filteredQuery.Where(book =>
                book.Title.Contains(searchText, StringComparison.CurrentCultureIgnoreCase));
        }

        if ((AuthorFilterComboBox.SelectedItem as FilterItem)?.Id is int authorId)
        {
            filteredQuery = filteredQuery.Where(book => book.Authors.Any(author => author.Id == authorId));
        }

        if ((GenreFilterComboBox.SelectedItem as FilterItem)?.Id is int genreId)
        {
            filteredQuery = filteredQuery.Where(book => book.Genres.Any(genre => genre.Id == genreId));
        }

        var filteredBooks = filteredQuery.ToList();
        BooksDataGrid.ItemsSource = filteredBooks;

        SummaryTextBlock.Text =
            $"Найдено книг: {filteredBooks.Count}. Экземпляров в наличии: {filteredBooks.Sum(book => book.QuantityInStock)}.";
    }

    private void AddBookButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new BookEditWindow
        {
            Owner = this
        };

        if (window.ShowDialog() == true)
        {
            ReloadData();
        }
    }

    private void EditBookButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (BooksDataGrid.SelectedItem is not Book selectedBook)
        {
            MessageBox.Show("Выберите книгу для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var window = new BookEditWindow(selectedBook.Id)
        {
            Owner = this
        };

        if (window.ShowDialog() == true)
        {
            ReloadData();
        }
    }

    private void DeleteBookButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (BooksDataGrid.SelectedItem is not Book selectedBook)
        {
            MessageBox.Show("Выберите книгу для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Удалить книгу \"{selectedBook.Title}\"?",
            "Подтверждение удаления",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        using var dbContext = LibraryDbContextFactory.Create();
        var book = dbContext.Books.FirstOrDefault(entity => entity.Id == selectedBook.Id);
        if (book is null)
        {
            ReloadData();
            return;
        }

        dbContext.Books.Remove(book);
        dbContext.SaveChanges();
        ReloadData();
    }

    private void ManageAuthorsButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new AuthorManagementWindow
        {
            Owner = this
        };

        window.ShowDialog();
        ReloadData();
    }

    private void ManageGenresButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new GenreManagementWindow
        {
            Owner = this
        };

        window.ShowDialog();
        ReloadData();
    }

    private void RefreshButton_OnClick(object sender, RoutedEventArgs e)
    {
        ReloadData();
    }

    private void SearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void FilterComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilters();
    }

    private sealed record FilterItem(int? Id, string Name);
}

