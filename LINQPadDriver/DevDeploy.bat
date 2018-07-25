rem
rem  You can simplify development by updating this batch file and then calling it from the 
rem  project's post-build event.
rem
rem  It copies the output .DLL (and .PDB) to LINQPad's drivers folder, so that LINQPad
rem  picks up the drivers immediately (without needing to click 'Add Driver').
rem
rem  NB: The target directory may not be correct for your computer!
rem  You can obtain the first part of the directory by running the following query:
rem
rem    Path.Combine (
rem       Environment.GetFolderPath (Environment.SpecialFolder.CommonApplicationData),
rem       @"LINQPad\Drivers\DataContext\3.5\")   
rem
rem  The final part of the directory is the name of the assembly plus its public key token in brackets.

rem kill LINQPad, which locks some of the assemblies we're trying to update
pskill LINQPad.exe

rem sleep for 2 seconds while LINQPad dies/releases its locks
ping 127.0.0.1 -n 3 -w 1000 >nul: 2>nul:

xcopy /i/y Tridion.ContentManager.CoreService.Client.dll "c:\ProgramData\LINQPad\Drivers\DataContext\4.0\TcmLINQPadDriver (02aa41e7a18c2f4e)\"
xcopy /i/y TcmTools.dll "c:\ProgramData\LINQPad\Drivers\DataContext\4.0\TcmLINQPadDriver (02aa41e7a18c2f4e)\"
REM xcopy /i/y Tidynet.dll "c:\ProgramData\LINQPad\Drivers\DataContext\4.0\TcmLINQPadDriver (02aa41e7a18c2f4e)\"
xcopy /i/y TcmLINQPadDriver.dll "c:\ProgramData\LINQPad\Drivers\DataContext\4.0\TcmLINQPadDriver (02aa41e7a18c2f4e)\"
xcopy /i/y TcmLINQPadDriver.pdb "c:\ProgramData\LINQPad\Drivers\DataContext\4.0\TcmLINQPadDriver (02aa41e7a18c2f4e)\"
xcopy /i/y Connection*.png "c:\ProgramData\LINQPad\Drivers\DataContext\4.0\TcmLINQPadDriver (02aa41e7a18c2f4e)\"

rem start C:\tools\LINQPad.exe