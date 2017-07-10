@echo off

set gac=C:\Windows\Microsoft.NET\assembly\GAC_MSIL
set csc=@c:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe

set acm=Altiris.Common
set acc=Altiris.AssetContractCommon
set ans=Altiris.NS
set ars=Altiris.Resource
set adb=Altiris.Database
set asi=Altiris.NS.StandardItems
set ats=Altiris.TaskManagement
set atc=Altiris.TaskManagement.Common
set apm=Altiris.PatchManagementCore
set apw=Altiris.PatchManagementCore.Web
set adn=Altiris.DotNetLib
set airm=Altiris.InventoryRuleManagement
set asm=Altiris.SoftwareManagement

set no_obs=find /v /i "is obsolete"
set no_pol=find /v /i "need to supply runtime policy"
set no_prv=find /v /i "related to previous warning"

if "%1"=="7.1" goto build-7.1
if "%1"=="7.5" goto build-7.5
if "%1"=="7.6" goto build-7.6
if "%1"=="8.0" goto build-8.0

:build-8.1

set ver1=v4.0_8.1.4528.0__d516cb311cfb6e4f
set atrscm=%acm%\%ver1%\%acm%
set atrsns=%ans%\%ver1%\%ans%
set atrsrx=%ars%\%ver1%\%ars%
set atrssi=%asi%\%ver1%\%asi%

set ver2=v4.0_8.1.4511.0__99b1e4cc0d03f223
set atrstm=%ats%\%ver2%\%ats%
set atrstc=%atc%\%ver2%\%atc%
set atrsdn=%adn%\%ver2%\%adn%

set ver3=v4.0_8.1.4538.0__d516cb311cfb6e4f
set atrspm=%apm%\%ver3%\%apm%

set ver4=v4.0_8.1.4502.0__d516cb311cfb6e4f
set  invrm=%airm%\%ver4%\%airm%

set ver5=v4.0_8.1.4508.0__d516cb311cfb6e4f
set  softm=%asm%\%ver5%\%asm%

set fullref=/reference:%gac%\%softm%.dll /reference:%gac%\%invrm%.dll /reference:%gac%\%atrscm%.dll /reference:%gac%\%atrsns%.dll /reference:%gac%\%atrsrx%.dll /reference:%gac%\%adb%\%ver1%\%adb%.dll /reference:%gac%\%atrssi%.dll /reference:%gac%\%atrstm%.dll /reference:%gac%\%atrspm%.dll /reference:%gac%\%atrstc%.dll /reference:%gac%\%atrsdn%.dll

set id=8.1

cmd /c %csc% %fullref% /out:ZeroDayPatch-%id%.exe ZeroDayPatch.cs Constant.cs APIWrapper.cs CLIConfig.cs CLIInit.cs | %no_obs% | %no_pol% | %no_prv%
cmd /c %csc% %fullref% /out:PatchAutomation-%id%.exe PatchAutomation.cs Constant.cs APIWrapper.cs CLIConfig.cs CLIInit.cs | %no_obs% | %no_pol% | %no_prv%
cmd /c %csc% %fullref% /out:PatchExclusion-%id%.exe patchexclusion.cs APIWrapper.cs Constant.cs | %no_obs% | %no_pol% | %no_prv%

goto end

:build-7.6

set ver1=v4.0_7.6.1383.0__d516cb311cfb6e4f
set ver2=v4.0_7.6.1395.0__d516cb311cfb6e4f
set ver3=v4.0_7.6.1383.0__99b1e4cc0d03f223

cmd /c %csc% /reference:%gac%\%asm%\%ver1%\%asm%.dll /reference:%gac%\%airm%\%ver1%\%airm%.dll /reference:%gac%\%acm%\%ver1%\%acm%.dll /reference:%gac%\%acc%\%ver2%\%acc%.dll /reference:%gac%\%ans%\%ver1%\%ans%.dll /reference:%gac%\%ars%\%ver1%\%ars%.dll /reference:%gac%\%adb%\%ver1%\%adb%.dll /reference:%gac%\%asi%\%ver1%\%asi%.dll /reference:%gac%\%ats%\%ver3%\%ats%.dll /reference:%gac%\%apm%\%ver2%\%apm%.dll /reference:%gac%\%atc%\%ver3%\%atc%.dll /reference:%gac%\%adn%\%ver3%\%adn%.dll /out:ZeroDayPatch-7.6.exe ZeroDayPatch.cs Constant.cs APIWrapper.cs CLIConfig.cs CLIInit.cs | %no_obs% | %no_pol% | %no_prv%

cmd /c %csc% /reference:%gac%\%asm%\%ver1%\%asm%.dll /reference:%gac%\%airm%\%ver1%\%airm%.dll /reference:%gac%\%acm%\%ver1%\%acm%.dll /reference:%gac%\%acc%\%ver2%\%acc%.dll /reference:%gac%\%ans%\%ver1%\%ans%.dll /reference:%gac%\%ars%\%ver1%\%ars%.dll /reference:%gac%\%adb%\%ver1%\%adb%.dll /reference:%gac%\%asi%\%ver1%\%asi%.dll /reference:%gac%\%ats%\%ver3%\%ats%.dll /reference:%gac%\%apm%\%ver2%\%apm%.dll /reference:%gac%\%atc%\%ver3%\%atc%.dll /reference:%gac%\%adn%\%ver3%\%adn%.dll /out:PatchAutomation-7.6.exe PatchAutomation.cs Constant.cs APIWrapper.cs CLIConfig.cs CLIInit.cs | %no_obs% | %no_pol% | %no_prv%

cmd /c %csc% /reference:%gac%\%asm%\%ver1%\%asm%.dll /reference:%gac%\%airm%\%ver1%\%airm%.dll /reference:%gac%\%acm%\%ver1%\%acm%.dll /reference:%gac%\%acc%\%ver2%\%acc%.dll /reference:%gac%\%ans%\%ver1%\%ans%.dll /reference:%gac%\%ars%\%ver1%\%ars%.dll /reference:%gac%\%adb%\%ver1%\%adb%.dll /reference:%gac%\%asi%\%ver1%\%asi%.dll /reference:%gac%\%ats%\%ver3%\%ats%.dll /reference:%gac%\%apm%\%ver2%\%apm%.dll /reference:%gac%\%atc%\%ver3%\%atc%.dll /reference:%gac%\%adn%\%ver3%\%adn%.dll /out:PatchExclusion-7.6.exe patchexclusion.cs APIWrapper.cs Constant.cs | %no_obs% | %no_pol% | %no_prv%

goto end

:build-7.5
set ver1=7.5.3153.0__d516cb311cfb6e4f
set ver2=7.5.3219.0__d516cb311cfb6e4f
set ver3=7.5.3153.0__99b1e4cc0d03f223

set gac=C:\Windows\Assembly\GAC_MSIL

cmd /c %csc% /reference:%gac%\%asm%\%ver1%\%asm%.dll /reference:%gac%\%airm%\%ver1%\%airm%.dll /reference:%gac%\%acm%\%ver1%\%acm%.dll /reference:%gac%\%acc%\%ver2%\%acc%.dll /reference:%gac%\%ans%\%ver1%\%ans%.dll /reference:%gac%\%ars%\%ver1%\%ars%.dll /reference:%gac%\%adb%\%ver1%\%adb%.dll /reference:%gac%\%asi%\%ver1%\%asi%.dll /reference:%gac%\%ats%\%ver3%\%ats%.dll /reference:%gac%\%apm%\%ver2%\%apm%.dll /reference:%gac%\%atc%\%ver3%\%atc%.dll /reference:%gac%\%adn%\%ver3%\%adn%.dll /out:ZeroDayPatch-7.5.exe ZeroDayPatch.cs Constant.cs APIWrapper.cs CLIConfig.cs CLIInit.cs | %no_obs% | %no_pol% | %no_prv%

cmd /c %csc% /reference:%gac%\%asm%\%ver1%\%asm%.dll /reference:%gac%\%airm%\%ver1%\%airm%.dll /reference:%gac%\%acm%\%ver1%\%acm%.dll /reference:%gac%\%acc%\%ver2%\%acc%.dll /reference:%gac%\%ans%\%ver1%\%ans%.dll /reference:%gac%\%ars%\%ver1%\%ars%.dll /reference:%gac%\%adb%\%ver1%\%adb%.dll /reference:%gac%\%asi%\%ver1%\%asi%.dll /reference:%gac%\%ats%\%ver3%\%ats%.dll /reference:%gac%\%apm%\%ver2%\%apm%.dll /reference:%gac%\%atc%\%ver3%\%atc%.dll /reference:%gac%\%adn%\%ver3%\%adn%.dll /out:PatchAutomation-7.5.exe PatchAutomation.cs Constant.cs APIWrapper.cs CLIConfig.cs CLIInit.cs | %no_obs% | %no_pol% | %no_prv%

cmd /c %csc% /reference:%gac%\%asm%\%ver1%\%asm%.dll /reference:%gac%\%airm%\%ver1%\%airm%.dll /reference:%gac%\%acm%\%ver1%\%acm%.dll /reference:%gac%\%acc%\%ver2%\%acc%.dll /reference:%gac%\%ans%\%ver1%\%ans%.dll /reference:%gac%\%ars%\%ver1%\%ars%.dll /reference:%gac%\%adb%\%ver1%\%adb%.dll /reference:%gac%\%asi%\%ver1%\%asi%.dll /reference:%gac%\%ats%\%ver3%\%ats%.dll /reference:%gac%\%apm%\%ver2%\%apm%.dll /reference:%gac%\%atc%\%ver3%\%atc%.dll /reference:%gac%\%adn%\%ver3%\%adn%.dll /out:PatchExclusion-7.5.exe patchexclusion.cs APIWrapper.cs Constant.cs | %no_obs% | %no_pol% | %no_prv%

goto end

:build-7.1
set ver1=7.1.8400.0__d516cb311cfb6e4f
set ver2=7.1.7858.0__d516cb311cfb6e4f
set ver3=7.1.8400.0__99b1e4cc0d03f223

set gac=C:\Windows\Assembly\GAC_MSIL
set csc=@c:\Windows\Microsoft.NET\Framework\v2.0.50727\csc.exe

cmd /c %csc% /reference:%gac%\%asm%\%ver1%\%asm%.dll /reference:%gac%\%airm%\%ver1%\%airm%.dll /reference:%gac%\%acm%\%ver1%\%acm%.dll /reference:%gac%\%ans%\%ver1%\%ans%.dll /reference:%gac%\%ars%\%ver1%\%ars%.dll /reference:%gac%\%adb%\%ver1%\%adb%.dll /reference:%gac%\%asi%\%ver1%\%asi%.dll /reference:%gac%\%ats%\%ver3%\%ats%.dll /reference:%gac%\%apm%\%ver2%\%apm%.dll /reference:%gac%\%atc%\%ver3%\%atc%.dll /reference:%gac%\%adn%\%ver3%\%adn%.dll /out:ZeroDayPatch-7.1.exe ZeroDayPatch.cs Constant.cs APIWrapper-7.1.cs CLIConfig.cs CLIInit.cs

cmd /c %csc% /reference:%gac%\%asm%\%ver1%\%asm%.dll /reference:%gac%\%airm%\%ver1%\%airm%.dll /reference:%gac%\%acm%\%ver1%\%acm%.dll /reference:%gac%\%ans%\%ver1%\%ans%.dll /reference:%gac%\%ars%\%ver1%\%ars%.dll /reference:%gac%\%adb%\%ver1%\%adb%.dll /reference:%gac%\%asi%\%ver1%\%asi%.dll /reference:%gac%\%ats%\%ver3%\%ats%.dll /reference:%gac%\%apm%\%ver2%\%apm%.dll /reference:%gac%\%atc%\%ver3%\%atc%.dll /reference:%gac%\%adn%\%ver3%\%adn%.dll /out:PatchAutomation-7.1.exe PatchAutomation.cs Constant.cs APIWrapper-7.1.cs CLIConfig.cs CLIInit.cs

cmd /c %csc% /reference:%gac%\%asm%\%ver1%\%asm%.dll /reference:%gac%\%airm%\%ver1%\%airm%.dll /reference:%gac%\%acm%\%ver1%\%acm%.dll /reference:%gac%\%ans%\%ver1%\%ans%.dll /reference:%gac%\%ars%\%ver1%\%ars%.dll /reference:%gac%\%adb%\%ver1%\%adb%.dll /reference:%gac%\%asi%\%ver1%\%asi%.dll /reference:%gac%\%ats%\%ver3%\%ats%.dll /reference:%gac%\%apm%\%ver2%\%apm%.dll /reference:%gac%\%atc%\%ver3%\%atc%.dll /reference:%gac%\%adn%\%ver3%\%adn%.dll /out:PatchExclusion-7.1.exe patchexclusion.cs APIWrapper-7.1.cs Constant.cs


:build-8.0

set ver1=v4.0_8.0.2240.0__d516cb311cfb6e4f
set ver2=v4.0_8.0.2221.0__99b1e4cc0d03f223
set ver3=v4.0_8.0.2217.0__d516cb311cfb6e4f
set ver4=v4.0_8.0.2202.0__d516cb311cfb6e4f
set ver5=v4.0_8.0.2215.0__d516cb311cfb6e4f

set invrm=%airm%\%ver4%\%airm%
set  softm=%asm%\%ver5%\%asm%
set atrscm=%acm%\%ver1%\%acm%
set atrsns=%ans%\%ver1%\%ans%
set atrsrx=%ars%\%ver1%\%ars%
set atrstm=%ats%\%ver2%\%ats%
set atrstc=%atc%\%ver2%\%atc%
set atrsdn=%adn%\%ver2%\%adn%
set atrspm=%apm%\%ver3%\%apm%

set fullref=/reference:%gac%\%softm%.dll /reference:%gac%\%invrm%.dll /reference:%gac%\%atrscm%.dll /reference:%gac%\%atrsns%.dll /reference:%gac%\%atrsrx%.dll /reference:%gac%\%adb%\%ver1%\%adb%.dll /reference:%gac%\%asi%\%ver1%\%asi%.dll /reference:%gac%\%atrstm%.dll /reference:%gac%\%atrspm%.dll /reference:%gac%\%atrstc%.dll /reference:%gac%\%atrsdn%.dll


set id=8.0

cmd /c %csc% %fullref% /out:ZeroDayPatch-%id%.exe ZeroDayPatch.cs Constant.cs APIWrapper.cs CLIConfig.cs CLIInit.cs | %no_obs% | %no_pol% | %no_prv%

cmd /c %csc% %fullref% /out:PatchAutomation-%id%.exe PatchAutomation.cs Constant.cs APIWrapper.cs CLIConfig.cs CLIInit.cs | %no_obs% | %no_pol% | %no_prv%

cmd /c %csc% %fullref% /out:PatchExclusion-%id%.exe patchexclusion.cs APIWrapper.cs Constant.cs | %no_obs% | %no_pol% | %no_prv%

cmd /c %csc% %fullref% /out:EnableBulletinByName-%id%.exe EnableBulletinByName.cs APIWrapper.cs

cmd /c %csc% %fullref% /out:GetDisabledBulletins-%id%.exe GetDisabledBulletins.cs
goto end

:end
