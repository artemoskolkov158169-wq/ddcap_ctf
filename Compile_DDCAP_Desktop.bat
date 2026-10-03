@echo off
title Compiling DDCAP Desktop (.NET 4.8 / C# 5)
echo [*] Compiling DDCAP sources from src\...
"D:\ProdCTF\compilers_latest\tasks\net472\csc.exe" -platform:anycpu -langversion:latest -target:winexe -out:"D:\ProdCTF\DDCAP-Desktop.exe" -lib:"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF","D:\ProdCTF\lib" -r:PresentationCore.dll -r:PresentationFramework.dll -r:WindowsBase.dll -r:System.Xaml.dll -r:System.dll -r:System.Core.dll -r:System.Net.Http.dll -r:System.Web.Extensions.dll -r:System.Windows.Forms.dll -r:"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\netstandard.dll" -r:"D:\ProdCTF\lib\SharpPcap.dll" -r:"D:\ProdCTF\lib\PacketDotNet.dll" -r:"D:\ProdCTF\lib\System.Runtime.CompilerServices.Unsafe.dll" -r:"D:\ProdCTF\lib\System.Memory.dll" -r:"D:\ProdCTF\lib\System.Buffers.dll" -r:"D:\ProdCTF\lib\System.Text.Encoding.CodePages.dll" -r:"D:\ProdCTF\lib\System.Numerics.Vectors.dll" -recurse:"D:\ProdCTF\src\*.cs"
if %errorlevel% equ 0 (
    echo [+] DDCAP-Desktop.exe successfully compiled!
    
    echo [*] Copying dependencies to Release folders...
    if not exist "D:\ProdCTF\Release" mkdir "D:\ProdCTF\Release"
    if not exist "D:\ProdCTF\DDCAP-Builder\Release" mkdir "D:\ProdCTF\DDCAP-Builder\Release"
    
    copy /Y "D:\ProdCTF\DDCAP-Desktop.exe" "D:\ProdCTF\Release\DDCAP-Desktop.exe" > nul
    copy /Y "D:\ProdCTF\DDCAP-Desktop.exe" "D:\ProdCTF\DDCAP-Builder\Release\DDCAP-Desktop.exe" > nul
    copy /Y "D:\ProdCTF\DDCAP-Desktop.exe.config" "D:\ProdCTF\Release\DDCAP-Desktop.exe.config" > nul
    copy /Y "D:\ProdCTF\DDCAP-Desktop.exe.config" "D:\ProdCTF\DDCAP-Builder\Release\DDCAP-Desktop.exe.config" > nul
    
    copy /Y "D:\ProdCTF\lib\SharpPcap.dll" "D:\ProdCTF\Release\" > nul
    copy /Y "D:\ProdCTF\lib\PacketDotNet.dll" "D:\ProdCTF\Release\" > nul
    copy /Y "D:\ProdCTF\lib\System.Runtime.CompilerServices.Unsafe.dll" "D:\ProdCTF\Release\" > nul
    copy /Y "D:\ProdCTF\lib\System.Memory.dll" "D:\ProdCTF\Release\" > nul
    copy /Y "D:\ProdCTF\lib\System.Buffers.dll" "D:\ProdCTF\Release\" > nul
    copy /Y "D:\ProdCTF\lib\System.Text.Encoding.CodePages.dll" "D:\ProdCTF\Release\" > nul
    copy /Y "D:\ProdCTF\lib\System.Numerics.Vectors.dll" "D:\ProdCTF\Release\" > nul
    copy /Y "D:\ProdCTF\lib\System.Threading.Tasks.Extensions.dll" "D:\ProdCTF\Release\" > nul
    if exist "D:\ProdCTF\lib\wpcap.dll" copy /Y "D:\ProdCTF\lib\wpcap.dll" "D:\ProdCTF\Release\" > nul
    if exist "D:\ProdCTF\lib\Packet.dll" copy /Y "D:\ProdCTF\lib\Packet.dll" "D:\ProdCTF\Release\" > nul
    
    copy /Y "D:\ProdCTF\lib\SharpPcap.dll" "D:\ProdCTF\DDCAP-Builder\Release\" > nul
    copy /Y "D:\ProdCTF\lib\PacketDotNet.dll" "D:\ProdCTF\DDCAP-Builder\Release\" > nul
    copy /Y "D:\ProdCTF\lib\System.Runtime.CompilerServices.Unsafe.dll" "D:\ProdCTF\DDCAP-Builder\Release\" > nul
    copy /Y "D:\ProdCTF\lib\System.Memory.dll" "D:\ProdCTF\DDCAP-Builder\Release\" > nul
    copy /Y "D:\ProdCTF\lib\System.Buffers.dll" "D:\ProdCTF\DDCAP-Builder\Release\" > nul
    copy /Y "D:\ProdCTF\lib\System.Text.Encoding.CodePages.dll" "D:\ProdCTF\DDCAP-Builder\Release\" > nul
    copy /Y "D:\ProdCTF\lib\System.Numerics.Vectors.dll" "D:\ProdCTF\DDCAP-Builder\Release\" > nul
    copy /Y "D:\ProdCTF\lib\System.Threading.Tasks.Extensions.dll" "D:\ProdCTF\DDCAP-Builder\Release\" > nul
    if exist "D:\ProdCTF\lib\wpcap.dll" copy /Y "D:\ProdCTF\lib\wpcap.dll" "D:\ProdCTF\DDCAP-Builder\Release\" > nul
    if exist "D:\ProdCTF\lib\Packet.dll" copy /Y "D:\ProdCTF\lib\Packet.dll" "D:\ProdCTF\DDCAP-Builder\Release\" > nul
    
    echo [*] Packing ZIP archives...
    powershell -NoProfile -Command "Compress-Archive -Path 'D:\ProdCTF\DDCAP-Builder' -DestinationPath 'D:\ProdCTF\DDCAP-Builder.zip' -Force; Compress-Archive -Path 'D:\ProdCTF\DDCAP-Builder\*' -DestinationPath 'D:\ProdCTF\DDCAP-Installer.zip' -Force"
    echo [+] DDCAP-Builder.zip and DDCAP-Installer.zip created!
    echo [+] All files ready in Release folders!
) else (
    echo [x] Compilation failed!
)
pause
