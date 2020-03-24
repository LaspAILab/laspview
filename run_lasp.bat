set path=%cd%/%1;%path%
CD %2
Start /B lasp.exe >output.txt 2>&1 