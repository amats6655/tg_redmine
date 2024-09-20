
# Telegram Redmine Notification Bot

![GitHub repo size](https://img.shields.io/github/repo-size/amats6655/tg_redmine) ![GitHub issues](https://img.shields.io/github/issues/amats6655/tg_redmine)

Telegram Redmine Notification Bot предназначен для уведомления о заявках, созданных в системе Redmine, с использованием представлений в базе данных. Бот отправляет уведомления о заявках в нужные чаты Telegram, автоматически обновляет информацию в сообщениях при изменении заявок и удаляет сообщения, связанные с закрытыми заявками.

## Основные возможности

- **Уведомления о новых заявках**: бот отправляет уведомления о новых заявках в назначенные чаты Telegram.
- **Обновление информации о заявках**: если заявка обновляется в базе данных, бот автоматически обновляет соответствующее сообщение в чате.
- **Удаление сообщений о закрытых заявках**: при закрытии заявки в базе данных бот удаляет соответствующее сообщение в чате.
- **Использование представлений базы данных**: бот получает всю необходимую информацию из специально созданных представлений в базе данных, что позволяет эффективно управлять данными без использования API Redmine.

## Структура проекта

- **TelegramBotService**: основная логика взаимодействия с Telegram API, отправка и обновление сообщений.
- **HostedService**: отвечает за основную логику обработки заявок, их обновление и удаление.
- **NotificationService**: сервис для отправки уведомлений в чаты Telegram.
- **IssueRepository**: получение данных о заявках из представлений базы данных.
- **MessageRepository**: управление историей сообщений, отправленных ботом в чаты.
- **UserRepository**: управление информацией о пользователях.

## Настройка и установка

1. **Клонирование репозитория**

    ```bash
    git clone https://github.com/amats6655/tg_redmine.git
    cd tg_redmine/tg_redmine
    ```

2. **Настройка `appsettings.json`**

    Пример файла `appsettings.json`:

    ```json
    {
      "TelegramSettings": {
        "Token": "YOUR_BOT_TOKEN"
      },
      "ConnectionStrings": {
        "DefaultConnection": "Data Source=RedmineBot.db",
        "IssuesViewConnection": "YOUR_DB_CONNECTION_STRING"
      },
      "HostingSettings": {
        "RequestFrequency": 100
      },
      "Serilog": {
        "MinimumLevel": {
          "Default": "Information",
          "Override": {
            "Microsoft": "Warning"
          }
        },
        "WriteTo": [
          {
            "Name": "Console"
          },
          {
            "Name": "File",
            "Args": {
              "path": "logs/log-.log",
              "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}",
              "rollingInterval": "Day",
              "retainedFileCountLimit": 90,
              "shared": true
            }
          }
        ],
        "Enrich": [ "FromLogContext" ]
      }
    }
    ```

    - **TelegramSettings.Token**: Токен вашего Telegram бота.
    - **ConnectionStrings.DefaultConnection**: Путь к базе данных SQLite для бота.
    - **ConnectionStrings.IssuesViewConnection**: Строка подключения к базе данных с представлениями заявок.
    - **HostingSettings.RequestFrequency**: Частота запросов в секундах для обновления информации.

3. **Запуск проекта**

    Перед запуском проекта убедитесь, что все необходимые зависимости установлены, и выполните команду:

    ```bash
    dotnet run
    ```

4. **Миграции базы данных**

    Чтобы создать и применить миграции для базы данных, выполните следующие команды:

    ```bash
    dotnet ef migrations add InitialCreate
    dotnet ef database update
    ```

## Использование

- После запуска бот начнет мониторить базу данных на наличие новых заявок, обновлять информацию в сообщениях и удалять сообщения о закрытых заявках.
- Все действия бота будут логироваться в консоль и в файлы логов, указанные в настройках `appsettings.json`.

## Требования

- .NET 8.0 и выше
- База данных с настроенными представлениями заявок
- Telegram бот с токеном доступа

## Контакты

Если у вас есть вопросы или предложения, вы можете связаться с автором через GitHub Issues.
