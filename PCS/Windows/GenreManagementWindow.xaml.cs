using System.Windows;
using System.Windows.Controls;
using PCS.Data;
using PCS.Models;

namespace PCS.Windows;

public partial class GenreManagementWindow : Window
{
    private int? _editingGenreId;

    public GenreManagementWindow()
    {
        InitializeComponent();
        Loaded += GenreManagementWindow_OnLoaded;
    }

    private void GenreManagementWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        LoadGenres();
    }

    private void LoadGenres(int? selectGenreId = null)
    {
        using var dbContext = LibraryDbContextFactory.Create();

        var genres = dbContext.Genres
            .OrderBy(genre => genre.Name)
            .ToList();

        GenresDataGrid.ItemsSource = genres;

        if (genres.Count == 0)
        {
            ClearForm();
            return;
        }

        if (selectGenreId.HasValue)
        {
            var selectedGenre = genres.FirstOrDefault(genre => genre.Id == selectGenreId.Value);
            if (selectedGenre is not null)
            {
                GenresDataGrid.SelectedItem = selectedGenre;
                return;
            }
        }

        GenresDataGrid.SelectedIndex = 0;
    }

    private void GenresDataGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GenresDataGrid.SelectedItem is not Genre selectedGenre)
        {
            return;
        }

        _editingGenreId = selectedGenre.Id;
        NameTextBox.Text = selectedGenre.Name;
        DescriptionTextBox.Text = selectedGenre.Description ?? string.Empty;
    }

    private void CreateNewButton_OnClick(object sender, RoutedEventArgs e)
    {
        ClearForm();
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text.Trim();
        var description = DescriptionTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Введите название жанра.", "Валидация", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        using var dbContext = LibraryDbContextFactory.Create();

        Genre genreEntity;
        if (_editingGenreId.HasValue)
        {
            var existingGenre = dbContext.Genres.FirstOrDefault(genre => genre.Id == _editingGenreId.Value);
            if (existingGenre is null)
            {
                MessageBox.Show(
                    "Жанр не найден. Возможно, он был удален.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                LoadGenres();
                return;
            }

            genreEntity = existingGenre;
        }
        else
        {
            genreEntity = new Genre();
            dbContext.Genres.Add(genreEntity);
        }

        genreEntity.Name = name;
        genreEntity.Description = string.IsNullOrWhiteSpace(description) ? null : description;

        dbContext.SaveChanges();
        LoadGenres(genreEntity.Id);
    }

    private void DeleteButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (GenresDataGrid.SelectedItem is not Genre selectedGenre)
        {
            MessageBox.Show("Выберите жанр для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Удалить жанр \"{selectedGenre.Name}\"?\nКниги не будут удалены, но связь с этим жанром будет снята.",
            "Подтверждение удаления",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        using var dbContext = LibraryDbContextFactory.Create();
        var genreEntity = dbContext.Genres.FirstOrDefault(genre => genre.Id == selectedGenre.Id);
        if (genreEntity is null)
        {
            LoadGenres();
            return;
        }

        dbContext.Genres.Remove(genreEntity);
        dbContext.SaveChanges();
        LoadGenres();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ClearForm()
    {
        _editingGenreId = null;
        GenresDataGrid.SelectedItem = null;
        NameTextBox.Text = string.Empty;
        DescriptionTextBox.Text = string.Empty;
        NameTextBox.Focus();
    }
}

