@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

set "INSTALL_DIR=%~dp0"
set "TARGET=!INSTALL_DIR:%USERPROFILE%=%%USERPROFILE%%!"

echo ============================================
echo  FPTP 命令行工具 — 安装到 PATH
echo ============================================
echo.
echo  安装目录: %INSTALL_DIR%
echo.
echo  此操作会将以上目录添加到用户 PATH 环境变量。
echo  之后可在 cmd / PowerShell 中直接使用 fptp 命令。
echo.

choice /C YN /M "是否继续"
if errorlevel 2 exit /b

REM 获取当前用户 PATH，去掉尾部分号
for /f "tokens=2*" %%a in ('reg query HKCU\Environment /v Path 2^>nul') do set "USER_PATH=%%b"
if not defined USER_PATH set "USER_PATH="

REM 检查是否已存在
echo !USER_PATH! | find /i "!TARGET!" >nul
if not errorlevel 1 (
    echo 该目录已在 PATH 中，无需重复添加。
    goto :done
)

REM 追加
set "NEW_PATH=!USER_PATH!;!TARGET!"
reg add HKCU\Environment /v Path /t REG_EXPAND_SZ /d "!NEW_PATH!" /f >nul
if errorlevel 1 (
    echo 添加失败，请以管理员身份运行。
    exit /b 1
)

echo 添加成功。
echo.

:done
echo 请重启终端或运行以下命令使其生效：
echo   for %%i in (refreshenv) do @for /f "tokens=*" %%j in ('%%i') do @%%j
echo.
echo 之后即可在任何路径下使用：
echo   fptp -v
echo   fptp -i photo.jpg -o out.jpg -s 1

endlocal
