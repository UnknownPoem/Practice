echo on
set apkName=%1
set apkNameWithoutExtention=%apkName:~0,-4%
echo Apk Name is %apkNameWithoutExtention%
del /F /Q %apkNameWithoutExtention%-modify.apk
del /F /Q %apkNameWithoutExtention%-modify-resign.apk
rmdir /S /Q %apkNameWithoutExtention%

echo decoding %apkName%...
call apktool d -f %apkName%

echo modify assetbundles...
copy /Y .\modify\*.* %apkNameWithoutExtention%\assets\

echo rebuild apk
call apktool b %apkNameWithoutExtention% -o %apkNameWithoutExtention%-modify.apk

echo resign apk
java -jar .\apksigner.jar sign --ks .\lzhd.keystore --ks-key-alias lzhd --out %apkNameWithoutExtention%-modify-resign.apk %apkNameWithoutExtention%-modify.apk
