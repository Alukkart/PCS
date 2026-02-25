using System.Windows;
using System.Windows.Controls;
using PCS.Data;
using PCS.Models;

namespace PCS.Windows;

public partial class AuthorManagementWindow : Window
{
    private int? _editingAuthorId;

    public AuthorManagementWindow()
    {
        InitializeComponent();
        Loaded += AuthorManagementWindow_OnLoaded;
    }

    private void AuthorManagementWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        LoadAuthors();
    }

    private void LoadAuthors(int? selectAuthorId = null)
    {
        using var dbContext = LibraryDbContextFactory.Create();

        var authors = dbContext.Authors
            .OrderBy(author => author.LastName)
            .ThenBy(author => author.FirstName)
            .ToList();

        AuthorsDataGrid.ItemsSource = authors;

        if (authors.Count == 0)
        {
            ClearForm();
            return;
        }

        if (selectAuthorId.HasValue)
        {
            var selectedAuthor = authors.FirstOrDefault(author => author.Id == selectAuthorId.Value);
            if (selectedAuthor is not null)
            {
                AuthorsDataGrid.SelectedItem = selectedAuthor;
                return;
            }
        }

        AuthorsDataGrid.SelectedIndex = 0;
    }

    private void AuthorsDataGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AuthorsDataGrid.SelectedItem is not Author selectedAuthor)
        {
            return;
        }

        _editingAuthorId = selectedAuthor.Id;
        FirstNameTextBox.Text = selectedAuthor.FirstName;
        LastNameTextBox.Text = selectedAuthor.LastName;
        BirthDatePicker.SelectedDate = selectedAuthor.BirthDate;
        CountryTextBox.Text = selectedAuthor.Country;
    }

    private void CreateNewButton_OnClick(object sender, RoutedEventArgs e)
    {
        ClearForm();
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        var firstName = FirstNameTextBox.Text.Trim();
        var lastName = LastNameTextBox.Text.Trim();
        var country = CountryTextBox.Text.Trim();
        var birthDate = BirthDatePicker.SelectedDate;

        if (string.IsNullOrWhiteSpace(firstName))
        {
            MessageBox.Show("Введите имя автора.", "Валидация", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            MessageBox.Show("Введите фамилию автора.", "Валидация", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!birthDate.HasValue)
        {
            MessageBox.Show("Выберите дату рождения.", "Валидация", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            MessageBox.Show("Введите страну автора.", "Валидация", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        using var dbContext = LibraryDbContextFactory.Create();

        Author authorEntity;
        if (_editingAuthorId.HasValue)
        {
            var existingAuthor = dbContext.Authors.FirstOrDefault(author => author.Id == _editingAuthorId.Value);
            if (existingAuthor is null)
            {
                MessageBox.Show(
                    "Автор не найден. Возможно, он был удален.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                LoadAuthors();
                return;
            }

            authorEntity = existingAuthor;
        }
        else
        {
            authorEntity = new Author();
            dbContext.Authors.Add(authorEntity);
        }

        authorEntity.FirstName = firstName;
        authorEntity.LastName = lastName;
        authorEntity.BirthDate = birthDate.Value;
        authorEntity.Country = country;

        dbContext.SaveChanges();
        LoadAuthors(authorEntity.Id);
    }

    private void DeleteButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (AuthorsDataGrid.SelectedItem is not Author selectedAuthor)
        {
            MessageBox.Show("Выберите автора для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Удалить автора \"{selectedAuthor.FullName}\"?\nКниги не будут удалены, но связь с этим автором будет снята.",
            "Подтверждение удаления",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        using var dbContext = LibraryDbContextFactory.Create();
        var authorEntity = dbContext.Authors.FirstOrDefault(author => author.Id == selectedAuthor.Id);
        if (authorEntity is null)
        {
            LoadAuthors();
            return;
        }

        dbContext.Authors.Remove(authorEntity);
        dbContext.SaveChanges();
        LoadAuthors();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ClearForm()
    {
        _editingAuthorId = null;
        AuthorsDataGrid.SelectedItem = null;
        FirstNameTextBox.Text = string.Empty;
        LastNameTextBox.Text = string.Empty;
        BirthDatePicker.SelectedDate = null;
        CountryTextBox.Text = string.Empty;
        FirstNameTextBox.Focus();
    }
}

