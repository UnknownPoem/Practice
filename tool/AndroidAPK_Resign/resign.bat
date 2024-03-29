echo resign apk
java -jar .\bin\apksigner.jar sign --ks .\bin\debug.keystore --ks-key-alias androiddebugkey --out resigned.apk cn.jj.orig.apk.aligned.apk