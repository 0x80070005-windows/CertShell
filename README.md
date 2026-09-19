# CertShell

TUI-оболочка для работы с зашифрованными файлами CertGuard. Позволяет просматривать, добавлять, удалять и открывать зашифрованные файлы прямо из терминала.

Работает в связке с CertGuard — программой для шифрования файлов на базе AES-256-GCM.

## Возможности

- 📂 Навигация по зашифрованным файлам — перемещение по списку через Tab, выбор через Enter
- 🔓 Автоматическая расшифровка и открытие файлов
- 🔐 Двухфакторная аутентификация — логин/пароль и проверка сертификата
- 🗂️ Работа с директориями — переход между папками, добавление и удаление файлов
- 🛠️ Мастер первого запуска — настройка путей к сертификату и образу диска
- 🖥️ Поддержка Linux и Windows
- 🧩 Интеграция с CertGuard
- ⚙️ Конфигурация через JSON

## Требования

- .NET 8.0 SDK или .NET 8.0 Runtime
- CertGuard
- OpenSSL 1.1.1 или выше

### Linux

Для открытия файлов используются:

- `mpv` — видео
- `eog` — изображения
- `gedit` — текстовые файлы
- `gnome-terminal` — запуск CertGuard в отдельном окне

### Windows

Для открытия файлов можно использовать:

- `mpv` — видео и аудио
- `ImageGlass` — изображения
- `Блокнот` или `Notepad++` — текстовые файлы
- `Windows Terminal` — запуск команд и CertGuard

Windows поддерживается, но не протестирован.

## Установка зависимостей на Arch Linux

```bash
sudo pacman -S dotnet-sdk mpv eog gedit openssl gnome-terminal

Установка
Клонирование репозитория
bash

git clone https://github.com/0x80070005-windows/CertShell.git
cd CertShell

Сборка
bash

dotnet build -c Release

Публикация для Linux
bash

dotnet publish -c Release -r linux-x64 --self-contained true \
    -p:PublishSingleFile=true -o ./out

Публикация для Windows

Для Windows x64:
PowerShell

dotnet publish -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -o .\out

Для Windows ARM64:
PowerShell

dotnet publish -c Release -r win-arm64 --self-contained true `
    -p:PublishSingleFile=true -o .\out

После публикации исполняемый файл будет находиться в папке out.
Запуск в Linux
bash

./out/CertShell

Запуск в Windows
PowerShell

.\out\CertShell.exe

Создание сертификата OpenSSL

CertShell и CertGuard используют самоподписанный сертификат как физический фактор для расшифровки файлов.

Создай сертификат в отдельной папке:
bash

mkdir -p cert_build
cd cert_build

openssl req -x509 -newkey rsa:4096 -sha256 -days 3650 -nodes \
    -keyout ca.key \
    -out ca.crt \
    -subj "/C=RU/O=CertShell/CN=CertShell"

Параметры сертификата:
Параметр	Описание
-x509	Создание самоподписанного сертификата
-newkey rsa:4096	Создание RSA-ключа длиной 4096 бит
-sha256	Использование алгоритма SHA-256
-days 3650	Срок действия сертификата — 10 лет
-nodes	Приватный ключ не защищается паролем
-keyout ca.key	Файл приватного ключа
-out ca.crt	Файл сертификата
-subj	Общие данные сертификата

Скопируй файл ca.crt на съёмный носитель.

Приватный ключ ca.key не следует копировать на съёмный носитель. Храни его отдельно или удали после создания резервной копии.
Первый запуск

При первом запуске CertShell запустит мастер настройки. Он:

    попросит указать путь к сертификату ca.crt;
    попросит указать путь к образу виртуального диска;
    запросит логин и пароль;
    сохранит настройки в конфигурационном файле.

Пример конфигурации:
Linux
JSON

{
  "CertPath": "/path/to/certificate/ca.crt",
  "ImgPath": "/path/to/virtual_disk.img",
  "MountPoint": "/mnt/virtual_disk/"
}

Windows
JSON

{
  "CertPath": "D:\\CertShell\\ca.crt",
  "ImgPath": "C:\\CertShell\\virtual_disk.img",
  "MountPoint": "C:\\CertShell\\virtual_disk\\"
}

Пути необходимо заменить на реальные пути в системе пользователя.
Перезапуск мастера

Если нужно изменить пути к сертификату или образу диска:
Linux
bash

./out/CertShell --setup

Windows
PowerShell

.\out\CertShell.exe --setup

Использование

После успешного входа появляется приглашение командной строки.
Команды
Команда	Описание
open	Открыть TUI-список файлов
ls	Показать содержимое текущей директории
lsdir	Показать список директорий
cd	Перейти в директорию
cd ..	Выйти из текущей директории
add	Добавить файл
remove	Удалить файл
mount	Примонтировать виртуальный диск
clear	Очистить экран
exit	Завершить работу и удалить временные файлы
Навигация в TUI

    Tab — переход к следующему элементу
    Enter — выбор текущего элемента
    Ctrl+C — выход из списка

Если выбранный файл имеет расширение .enc, CertGuard расшифрует его во временную папку. После этого файл будет открыт с помощью программы, назначенной для соответствующего типа файла.
Архитектура
Text

CertShell/
├── Program.cs
├── Config/
│   └── AppConfig.cs
├── Security/
│   ├── CheckSecurity.cs
│   └── SetupWizard.cs
├── Commands/
│   ├── Commande.cs
│   ├── ChangeDirectory.cs
│   └── DefiningExtensions.cs
└── FileSelector/
    ├── FileSelectorUi.cs
    ├── CallOfCertguard.cs
    └── CallOfCertguardForChangeDirectory.cs

Как это работает

    SetupWizard.Run() запускает мастер первоначальной настройки.
    CheckSecurity.Init() выполняет проверку сертификата и учётных данных.
    Основной цикл принимает команды пользователя.
    Команда open запускает TUI-список файлов.
    CertShell отображает файлы из настроенной директории.
    Зашифрованные файлы с расширением .enc передаются CertGuard.
    Расшифрованный файл открывается с помощью подходящей программы.

Безопасность

CertShell использует многоуровневую защиту:

    логин и пароль;
    хранение учётных данных в виде SHA-512-хешей;
    проверку сертификата;
    хранение сертификата на съёмном носителе;
    шифрование AES-256-GCM и PBKDF2 в CertGuard;
    разделение мастер-ключа между сертификатом и системным файлом.

Рекомендации

    Не храните пароль в открытом виде.
    Используйте менеджер паролей.
    Храните резервную копию сертификата отдельно от компьютера.
    Не оставляйте расшифрованные файлы на диске после завершения работы.
    Используйте команду exit после просмотра файлов.
