set WORKSPACE=.
set LUBAN_DLL=%WORKSPACE%\Luban\Luban.dll
set CONF_ROOT=.

dotnet %LUBAN_DLL% ^
	-t all ^
    -c cs-bin ^
    -d bin  ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=output/Code ^
    -x outputDataDir=output/Data ^
	
pause