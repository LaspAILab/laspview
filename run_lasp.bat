set path=%cd%\LASP4win_release_x86_20191209;%path%
CD %2
echo %path%
Start /B Src.exe >output.txt 2>&1 