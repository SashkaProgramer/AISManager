@echo off
echo ВАЖНО! Не закрывайте это окно, дождитесь сообщения об окончании установки дополнений для АИС-3 ПРОМ!
set wttm=60
set nativemsg=%systemroot%\sysnative\msg.exe
set msghello=* /time:%wttm% "Установка дополнений для АИС-3 ПРОМ. Программа будет автоматически закрыта в течении %wttm% секунд, во время установки клиент будет недоступен (примерно 7 минут)."
set msgbye=* /time:24000 "Установка дополнений для АИС-3 ПРОМ завершена."
::
if exist "%ProgramFiles%\Ais3Prom\SystemVersionMetadata.xml" (
if exist %nativemsg% (%nativemsg% %msghello%) else (msg %msghello%)
echo Построение списка обновляемых файлов.
ping -n %wttm% -w 1000 localhost 2>NUL 1>&2
echo Копирование файлов.
taskkill.exe /F /T /IM CommonComponents.UnifiedClient.exe 2>NUL 1>&2
ping -n 10 -w 1000 localhost 2>NUL 1>&2
xcopy "%~dp0Client\*.*" "%ProgramFiles%\Ais3Prom\Client" /E /I /H /R /K /Y 2>NUL 1>&2
move "%ProgramFiles%\Ais3Prom\Client\CommonComponents.UnifiedClient.exe" "%ProgramFiles%\Ais3Prom\Client\CommonComponents.UnifiedClient.ex_" 2>NUL 1>&2
echo Индексация файлов.
"%ProgramFiles%\Ais3Prom\Client\CommonComponents.Catalog.IndexationUtility.exe" 2>NUL 1>&2
move "%ProgramFiles%\Ais3Prom\Client\CommonComponents.UnifiedClient.ex_" "%ProgramFiles%\Ais3Prom\Client\CommonComponents.UnifiedClient.exe" 2>NUL 1>&2
if exist %nativemsg% (%nativemsg% %msgbye%) else (msg %msgbye%)
)