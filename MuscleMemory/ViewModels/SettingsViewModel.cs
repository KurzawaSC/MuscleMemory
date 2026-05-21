using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Data;

namespace MuscleMemory.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly DatabaseContext _dbContext;

    public SettingsViewModel(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
    }

    // 1. KOMENDA: CZYSZCZENIE BAZY
    [RelayCommand]
    private async Task EraseDataAsync()
    {
        bool isConfirmed = await Shell.Current.DisplayAlert(
            "Warning!",
            "Are you sure you want to delete ALL your exercises and workouts? This action cannot be undone.",
            "Yes, erase it",
            "Cancel");

        if (isConfirmed)
        {
            await _dbContext.ClearAllDataAsync();
            await Shell.Current.DisplayAlert("Success", "All your data has been erased.", "OK");
        }
    }

    // 2. KOMENDA: EKSPORT DANYCH
    [RelayCommand]
    private async Task ExportDataAsync()
    {
        try
        {
            // Ścieżka do Twojego pliku z bazą SQLite
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "MuscleMemory.db3");

            if (!File.Exists(dbPath))
            {
                await Shell.Current.DisplayAlert("Oops!", "There is no data to export yet.", "OK");
                return;
            }

            // Otwiera systemowe okienko udostępniania (Share)
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Export Muscle Memory Data",
                File = new ShareFile(dbPath)
            });
        }
        catch (Exception ex)
        {
            // W razie braku uprawnień lub błędu
            await Shell.Current.DisplayAlert("Error", $"Failed to export data: {ex.Message}", "OK");
        }
    }
}