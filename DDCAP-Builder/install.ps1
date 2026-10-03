# DDCAP Defense Terminal - Auto-Installer
# For fresh Windows machines at Altay CTF
[CmdletBinding()]
param()

Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "     DDCAP DEFENSE TERMINAL // AUTO-INSTALLER        " -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Check for Administrator Privileges
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (!$isAdmin) {
    Write-Host "[!] Запустите этот скрипт от имени Администратора!" -ForegroundColor Red
    Write-Host "    Raw Sockets (сниффер) требуют прав Администратора." -ForegroundColor Red
    Read-Host "Нажмите Enter для выхода..."
    Exit
}

Write-Host "[+] Права Администратора подтверждены." -ForegroundColor Green

# 2. Check and Install Python
Write-Host "[*] Проверка наличия Python (нужен для фермы эксплойтов)..." -ForegroundColor Yellow
$pythonInstalled = $false
try {
    $pyVersion = & python --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[+] Python найден: $pyVersion" -ForegroundColor Green
        $pythonInstalled = $true
    }
} catch { }

if (!$pythonInstalled) {
    Write-Host "[!] Python не найден. Начинаю скачивание и установку (Python 3.11)..." -ForegroundColor Yellow
    $pythonUrl = "https://www.python.org/ftp/python/3.11.6/python-3.11.6-amd64.exe"
    $installerPath = "$env:TEMP\python-installer.exe"
    
    try {
        Invoke-WebRequest -Uri $pythonUrl -OutFile $installerPath
        Write-Host "[*] Установка Python в фоновом режиме..." -ForegroundColor Yellow
        Start-Process -FilePath $installerPath -ArgumentList "/quiet InstallAllUsers=1 PrependPath=1 Include_test=0" -Wait -NoNewWindow
        Write-Host "[+] Python успешно установлен и добавлен в PATH!" -ForegroundColor Green
        
        # Обновляем PATH в текущей сессии для проверки
        $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
} catch {
        Write-Host "[-] Ошибка при установке Python. Установите вручную." -ForegroundColor Red
    }
}

# 2.5 Check and Install WinPcap/Npcap (required for SharpPcap)
Write-Host "[*] Проверка наличия драйвера захвата пакетов (WinPcap/Npcap)..." -ForegroundColor Yellow
$pcapInstalled = (Test-Path "$env:windir\System32\wpcap.dll") -or (Test-Path "$env:windir\SysWOW64\wpcap.dll")
if (!$pcapInstalled) {
    Write-Host "[!] Драйвер не найден. Начинаю скачивание WinPcap..." -ForegroundColor Yellow
    $pcapUrl = "https://www.winpcap.org/install/bin/WinPcap_4_1_3.exe"
    $pcapInstallerPath = "$env:TEMP\winpcap-installer.exe"
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -Uri $pcapUrl -OutFile $pcapInstallerPath
        Write-Host "[*] Запуск установщика WinPcap. Пожалуйста, прокликайте 'Next' -> 'Install'!" -ForegroundColor Magenta
        Start-Process -FilePath $pcapInstallerPath -Wait
        Write-Host "[+] Установка драйвера захвата пакетов завершена!" -ForegroundColor Green
    } catch {
        Write-Host "[-] Ошибка при скачивании WinPcap. Установите Npcap или WinPcap вручную." -ForegroundColor Red
    }
} else {
    Write-Host "[+] Драйвер захвата пакетов (wpcap.dll) уже установлен!" -ForegroundColor Green
}

# 3. Create .env file if it doesn't exist
$envPath = "$PSScriptRoot\Release\.env"
if (!(Test-Path $envPath)) {
    Write-Host "[*] Создание файла конфигурации .env..." -ForegroundColor Yellow
    Set-Content -Path $envPath -Value "GEMINI_API_KEY=`nPOLZA_AI_API_KEY=`nDDCAP_LOCAL_IP="
}

# 4. Create Desktop Shortcut with RunAsAdmin flag
Write-Host "[*] Создание ярлыка на рабочем столе..." -ForegroundColor Yellow
try {
    $WshShell = New-Object -comObject WScript.Shell
    $DesktopPath = [Environment]::GetFolderPath("Desktop")
    $ShortcutPath = "$DesktopPath\DDCAP Terminal.lnk"
    
    $Shortcut = $WshShell.CreateShortcut($ShortcutPath)
    $Shortcut.TargetPath = "$PSScriptRoot\Release\DDCAP-Desktop.exe"
    $Shortcut.WorkingDirectory = "$PSScriptRoot\Release"
    $Shortcut.IconLocation = "$PSScriptRoot\Release\DDCAP-Desktop.exe,0"
    $Shortcut.Save()
    
    # Modify LNK file binary to force "Run as Administrator"
    $bytes = [IO.File]::ReadAllBytes($ShortcutPath)
    $bytes[21] = $bytes[21] -bor 0x20
    [IO.File]::WriteAllBytes($ShortcutPath, $bytes)
    
    Write-Host "[+] Ярлык создан на Рабочем столе!" -ForegroundColor Green

    # Копируем ярлык в Автозагрузку
    $StartupPath = [Environment]::GetFolderPath("Startup")
    Copy-Item $ShortcutPath -Destination "$StartupPath\DDCAP Terminal.lnk" -Force
    Write-Host "[+] Ярлык добавлен в Автозагрузку (Auto-start)!" -ForegroundColor Green

} catch {
    Write-Host "[-] Ошибка при создании ярлыка." -ForegroundColor Red
}

# 5. Add Firewall Rules
Write-Host "[*] Настройка исключений Windows Firewall..." -ForegroundColor Yellow
try {
    $exePath = "$PSScriptRoot\Release\DDCAP-Desktop.exe"
    # Удаляем старые правила если есть
    netsh advfirewall firewall delete rule name="DDCAP Terminal" program="$exePath" | Out-Null
    
    # Добавляем Inbound и Outbound правила
    netsh advfirewall firewall add rule name="DDCAP Terminal" dir=in action=allow program="$exePath" enable=yes profile=any | Out-Null
    netsh advfirewall firewall add rule name="DDCAP Terminal" dir=out action=allow program="$exePath" enable=yes profile=any | Out-Null
    Write-Host "[+] Правила Firewall успешно добавлены!" -ForegroundColor Green
} catch {
    Write-Host "[-] Ошибка при настройке Firewall." -ForegroundColor Red
}

Write-Host ""
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "УСТАНОВКА УСПЕШНО ЗАВЕРШЕНА!" -ForegroundColor Green
Write-Host "Вы можете запустить DDCAP Terminal с ярлыка на рабочем столе." -ForegroundColor Yellow
Write-Host "=====================================================" -ForegroundColor Cyan
Read-Host "Нажмите Enter для выхода..."
