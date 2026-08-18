# MuscleMemory 🏋️‍♂️

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)
![.NET MAUI](https://img.shields.io/badge/UI-.NET%20MAUI-512BD4?style=for-the-badge&logo=dotnet)
![C# 13](https://img.shields.io/badge/Language-C%23%2013-239120?style=for-the-badge&logo=csharp)
![Platform](https://img.shields.io/badge/Platform-Android-3DDC84?style=for-the-badge&logo=android)

**MuscleMemory** to nowoczesna, minimalistyczna i ultraszybka aplikacja mobilna do śledzenia treningów siłowych. Została zaprojektowana z myślą o czytelności, intuicyjnej obsłudze na siłowni oraz pełnej prywatności danych dzięki lokalnej bazie SQLite.

---

## 🚀 Główne Funkcje

- 📝 **Zarządzanie Treningami:** Twórz, edytuj i dostosowuj własne plany treningowe oraz listy ćwiczeń.
- ⏱️ **Aktywny Trening:** Śledź serie, powtórzenia i ciężary w czasie rzeczywistym z wygodnym interfejsem nastawionym na szybkie wprowadzanie danych.
- 📜 **Historia Treningów:** Przeglądaj archiwalne sesje i analizuj swój progres.
- 🎨 **Motyw Jasny / Ciemny / Systemowy:** W pełni dostosowany dynamiczny Dark Mode z czarnym paskiem nawigacji i wyrazistymi, kontrastowymi ikonami.
- 🖐️ **Intuicyjne Gesty:** Gest przesunięcia (SwipeView) z dostosowanymi, konturowymi przyciskami do szybkiej edycji i usuwania.
- 🔒 **Privacy-First:** Wszystkie dane przechowywane są lokalnie na Twoim urządzeniu.

---

## 🛠️ Stack Technologiczny

- **Framework:** [.NET 10 MAUI](https://learn.microsoft.com/dotnet/maui/)
- **Język:** C# 13 (AOT-compatible, partial properties syntax)
- **Wzorzec Projektowy:** MVVM (CommunityToolkit.Mvvm)
- **Baza Danych:** Local SQLite (`sqlite-net-e` + `SourceGear.sqlite3` 3.x z pełnym wsparciem dla Android 16KB Page Size)
- **UI & UX:** Custom XAML Controls, LilitaOne Typography, Border-based Card Layouts, System/Dark Theme Switching

---

## 📐 Architektura Projektu

Projekt został stworzony zgodnie z zasadami **Clean Architecture** i **Clean Code (DRY)**:

```text
MuscleMemory/
├── Models/             # Encje bazy danych (Workout, Exercise, WorkoutLog, etc.)
├── Data/               # Obsługa SQLite (DatabaseContext)
├── ViewModels/         # Logika aplikacji z użyciem CommunityToolkit.Mvvm
├── Views/              # Widoki XAML (Listy, Widok Treningu, Ustawienia, Popupy)
├── Resources/          # Czcionki (LilitaOne), Kolory, Style, Splash Screen, Ikony
├── AppShell.xaml       # Główna nawigacja Shell z obsługą TabBar
└── MauiProgram.cs      # Rejestracja usług i inicjalizacja natywnych bibliotek
⚙️ Wymagania i Uruchomienie
Wymagania:
SDK .NET 10 z zainstalowanym workloadem maui-android

Android Studio / Emulator Androida (API 26+) lub urządzenie fizyczne z włączonym trybem deweloperskim

Budowanie i uruchomienie z terminala:
Sklonuj repozytorium:

Bash
git clone [https://github.com/TwojUser/MuscleMemory.git](https://github.com/TwojUser/MuscleMemory.git)
cd MuscleMemory
Przywróć pakiety NuGet:

Bash
dotnet restore
Uruchom aplikację na emulatorze Androida:

Bash
dotnet build MuscleMemory/MuscleMemory.csproj -t:Run -f net10.0-android
