# -----------------------------
# 1. Сборка приложения (ARM64)
# -----------------------------
FROM --platform=linux/arm64 mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем решение и все проекты для корректного восстановления зависимостей
COPY Pubg_bot_restart.sln ./
COPY MyBot.csproj ./ 
# Если есть другие проекты, их тоже можно копировать отдельно
# COPY OtherProject.csproj ./OtherProject/

# Восстанавливаем зависимости по решению
RUN dotnet restore Pubg_bot_restart.sln

# Копируем весь исходный код
COPY . ./

# Публикуем через решение, указываем конкретный проект, если нужно
RUN dotnet publish Pubg_bot_restart.sln -c Release -o /app /p:StartupProject=MyBot

# -----------------------------
# 2. Финальный образ с Runtime (ARM64)
# -----------------------------
FROM --platform=linux/arm64 mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Копируем скомпилированное приложение из сборочного образа
COPY --from=build /app ./

#конфиг
COPY config.xml .

# Копируем папку с видео
COPY Video ./Video/
# Папки со стикерами
COPY Stickers ./Stickers/
# Папки с голосами
COPY Voices ./Voices/

# Указываем команду запуска
ENTRYPOINT ["dotnet", "MyBot.dll"]
