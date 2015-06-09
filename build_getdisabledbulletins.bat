@echo off

set gac=C:\Windows\Microsoft.NET\assembly\GAC_MSIL
set csc=@c:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe

set acm=Altiris.Common
set apm=Altiris.PatchManagementCore

if "%1"=="7.1" goto build-7.1
if "%1"=="7.5" goto build-7.5

:build-7.6

set ver1=v4.0_7.6.1383.0__d516cb311cfb6e4f
set ver2=v4.0_7.6.1395.0__d516cb311cfb6e4f
set ver3=v4.0_7.6.1383.0__99b1e4cc0d03f223

cmd /c %csc% /reference:%gac%\%acm%\%ver1%\%acm%.dll /reference:%gac%\%apm%\%ver2%\%apm%.dll /out:GetDisabledBulletins-7.6.exe GetDisabledBulletins.cs

goto end

:build-7.5
set ver1=7.5.3153.0__d516cb311cfb6e4f
set ver4=7.5.3125.0__d516cb311cfb6e4f
set ver2=7.5.3219.0__d516cb311cfb6e4f
set ver3=7.5.3153.0__99b1e4cc0d03f223

set csc=@c:\Windows\Microsoft.NET\Framework\v2.0.50727\csc.exe
set gac=C:\Windows\Assembly\GAC_MSIL

cmd /c %csc% /reference:%gac%\%acm%\%ver1%\%acm%.dll /reference:%gac%\%apm%\%ver2%\%apm%.dll /out:GetDisabledBulletins-7.5.exe GetDisabledBulletins.cs

goto end

:build-7.1
set ver1=7.1.8400.0__d516cb311cfb6e4f
set ver2=7.1.7858.0__d516cb311cfb6e4f
set ver3=7.1.8400.0__99b1e4cc0d03f223

set gac=C:\Windows\Assembly\GAC_MSIL
set csc=@c:\Windows\Microsoft.NET\Framework\v2.0.50727\csc.exe

cmd /c %csc% /reference:%gac%\%acm%\%ver1%\%acm%.dll /reference:%gac%\%apm%\%ver2%\%apm%.dll /out:GetDisabledBulletins-7.1.exe GetDisabledBulletins.cs

:end
